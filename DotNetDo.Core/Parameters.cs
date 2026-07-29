using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace DotNetDo;

public static partial class Do
{
    const string ImplicitBooleanValue = "\0";
    static readonly Lazy<IConfiguration> ParameterConfiguration = new(CreateParameterConfiguration);

    /// <summary>Declares a command-line parameter and resolves its configured value without executing user code during help discovery.</summary>
    public static OptionalParam<string> Param(string name) =>
        new(name, ReadParam<string>(name), null);

    /// <summary>Declares an optional typed command-line parameter without a default value.</summary>
    public static OptionalParam<T> Param<T>(string name) where T : notnull =>
        new(name, ReadParam<T>(name), null);

    /// <summary>Declares a command-line parameter and resolves its configured value without executing user code during help discovery.</summary>
    public static Param<T> Param<T>(string name, T defaultValue, string? description = null) where T : notnull =>
        new(name, ReadParam(name, defaultValue), description);

    /// <summary>Declares a string parameter whose resolved value is registered for log redaction.</summary>
    public static OptionalSecret Secret(string name, string? defaultValue = null, string? description = null) =>
        new(name, ReadSecret(name, defaultValue), description);

    static ParameterValue<T> ReadParam<T>(string name) =>
        ReadConfigurationValue<T>(name) is { HasValue: true } value
            ? value
            : ParameterValue<T>.Missing(name);

    static ParameterValue<T> ReadParam<T>(string name, T defaultValue) =>
        ReadConfigurationValue<T>(name) is { HasValue: true } value
            ? value
            : ParameterValue<T>.Resolved(name, defaultValue);

    static ParameterValue<string> ReadSecret(string name, string? defaultValue)
    {
        var value = ReadConfigurationValue<string>(name) is { HasValue: true } configured
            ? configured.Value
            : defaultValue;

        return value is null
            ? ParameterValue<string>.Missing(name)
            : ParameterValue<string>.Resolved(name, value);
    }

    static ParameterValue<T> ReadConfigurationValue<T>(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        switch (ParameterConfiguration.Value[name])
        {
            case null:
                return ParameterValue<T>.Missing(name);
            case ImplicitBooleanValue:
                return typeof(T) == typeof(bool)
                    ? ParameterValue<T>.Resolved(name, (T)(object)true)
                    : throw new InvalidOperationException($"Parameter '{name}' requires a value.");
            default:
                try
                {
                    return ParameterValue<T>.Resolved(name, ParameterConfiguration.Value.GetValue<T>(name)!);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"Parameter '{name}' could not be parsed as {typeof(T).Name}.", exception);
                }
        }
    }

    static IConfiguration CreateParameterConfiguration()
    {
        return new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetEntryAssembly() ?? typeof(Do).Assembly, optional: true)
            .AddEnvironmentVariables("DOTNETDO_")
            .AddCommandLine(NormalizeParameterArguments(Environment.GetCommandLineArgs().Skip(1)))
            .Build();
    }

    internal static string[] NormalizeParameterArguments(IEnumerable<string> arguments)
    {
        var values = arguments.ToArray();

        for (var index = 0; index < values.Length; index++)
        {
            var value = values[index];
            if (value.Length > 2
                && value.StartsWith("--", StringComparison.Ordinal)
                && !value.Contains('=')
                && (index == values.Length - 1 || values[index + 1].StartsWith("--", StringComparison.Ordinal)))
            {
                values[index] = $"{value}={ImplicitBooleanValue}";
            }
        }

        return values;
    }
}

/// <summary>A task parameter guaranteed to resolve from configuration or its default value.</summary>
public readonly record struct Param<T> where T : notnull
{
    readonly ParameterValue<T> _value;

    internal Param(string name, ParameterValue<T> value, string? description)
    {
        Name = name;
        _value = value;
        Description = description;
    }

    /// <summary>The final path component, or <see langword="null"/> for a root or empty path.</summary>
    public string Name { get; }
    /// <summary>Human-readable help text supplied by the task author.</summary>
    public string? Description { get; }
    /// <summary>The resolved parameter value.</summary>
    public T Value => _value.Value;

    /// <summary>Renders the resolved value as one quoted command-line argument.</summary>
    public string QuotedArgument() => Convert.ToString(_value.Value, CultureInfo.InvariantCulture)!.QuotedArgument();

    /// <summary>The resolved parameter value.</summary>
    public static implicit operator T(Param<T> parameter) => parameter.Value;
}

/// <summary>An optional string task parameter whose absence is represented by <see langword="null"/>.</summary>
public readonly record struct OptionalParam<T>
    where T : notnull
{
    readonly ParameterValue<T> _value;

    internal OptionalParam(string name, ParameterValue<T> value, string? description)
    {
        Name = name;
        _value = value;
        Description = description;
    }

    /// <summary>The parameter name.</summary>
    public string Name { get; }
    /// <summary>Human-readable help text supplied by the task author.</summary>
    public string? Description { get; }
    /// <summary>The resolved parameter value, or <see langword="null"/> when absent.</summary>
    public T? Value => _value.ValueOrDefault;

    /// <summary>Resolves the optional parameter, throwing when no value was supplied.</summary>
    public Param<T> Required() =>
        _value.HasValue
            ? new Param<T>(Name, _value, Description)
            : throw new InvalidOperationException($"Parameter '{Name}' is required.");

    /// <summary>Renders the resolved optional value as one quoted command-line argument.</summary>
    public string? QuotedArgument() => _value.HasValue ? Convert.ToString(_value.Value, CultureInfo.InvariantCulture)?.QuotedArgument() : null;

    /// <summary>The resolved parameter value.</summary>
    public static implicit operator T?(OptionalParam<T> parameter) => parameter.Value;
}

/// <summary>An optional string parameter that masks its value in text and logs.</summary>
public readonly record struct OptionalSecret
{
    readonly ParameterValue<string> _value;

    internal OptionalSecret(string name, ParameterValue<string> value, string? description)
    {
        Name = name;
        _value = value;
        Description = description;
        if (value.HasValue)
            SecretRedaction.Register(value.Value);
    }

    /// <summary>The parameter name, or <see langword="null"/> when constructed directly.</summary>
    public string? Name { get; }
    /// <summary>Human-readable help text supplied by the task author.</summary>
    public string? Description { get; }

    /// <summary>Returns the plaintext secret value; callers must avoid writing it to unredacted output.</summary>
    public string? Unwrap() => _value.ValueOrDefault;

    /// <summary>Renders the resolved optional secret value as one quoted command-line argument.</summary>
    public string? QuotedArgument() => Unwrap()?.QuotedArgument();

    /// <summary>Converts the optional parameter to its required form, throwing when no value was supplied.</summary>
    public Secret Required() =>
        _value.HasValue
            ? new Secret(Name, _value.Value, Description)
            : throw new InvalidOperationException($"Secret parameter '{Name}' is required.");

    /// <inheritdoc />
    public override string ToString() => "***";
}

/// <summary>A secret with an available plaintext value.</summary>
public readonly record struct Secret
{
    readonly string _value;

    /// <summary>Wraps and registers a plaintext value for log redaction.</summary>
    public Secret(string value)
        : this(null, value, null) { }

    internal Secret(string? name, string value, string? description)
    {
        ArgumentNullException.ThrowIfNull(value);
        Name = name;
        _value = value;
        Description = description;
        SecretRedaction.Register(value);
    }

    /// <summary>The final path component, or <see langword="null"/> for a root or empty path.</summary>
    public string? Name { get; }
    /// <summary>Human-readable help text supplied by the task author.</summary>
    public string? Description { get; }

    /// <summary>Returns the plaintext secret value; callers must avoid writing it to unredacted output.</summary>
    public string Unwrap() => _value;

    /// <summary>Renders the resolved secret value as one quoted command-line argument.</summary>
    public string QuotedArgument() => _value.QuotedArgument();

    /// <inheritdoc />
    public override string ToString() => "***";
}

readonly record struct ParameterValue<T>(string Name, bool HasValue, T Value)
{
    public T? ValueOrDefault => HasValue ? Value : default;

    public static ParameterValue<T> Missing(string name) => new(name, false, default!);

    public static ParameterValue<T> Resolved(string name, T value) => new(name, true, value);
}