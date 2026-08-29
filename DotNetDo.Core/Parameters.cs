using System.Globalization;

namespace DotNetDo;

public static partial class Do
{
    static readonly Lazy<TaskParameterConfiguration> ParameterConfiguration = new(() => TaskParameterConfiguration.Current);

    /// <summary>Declares a command-line parameter and resolves its configured value without executing user code during help discovery.</summary>
    /// <param name="name">The non-empty configuration key, written as <c>--name value</c> on the command line or <c>DOTNETDO_name</c> in the environment.</param>
    public static OptionalParam<string> Param(string name) =>
        new(name, ReadParam<string>(name), null);

    /// <summary>Declares an optional typed command-line parameter without a default value.</summary>
    /// <param name="name">The non-empty configuration key, written as <c>--name value</c> on the command line or <c>DOTNETDO_name</c> in the environment.</param>
    public static OptionalParam<T> Param<T>(string name) where T : notnull =>
        new(name, ReadParam<T>(name), null);

    /// <summary>Declares a command-line parameter and resolves its configured value without executing user code during help discovery.</summary>
    /// <param name="name">The non-empty configuration key, written as <c>--name value</c> on the command line or <c>DOTNETDO_name</c> in the environment.</param>
    /// <param name="defaultValue">The value used when command-line arguments, environment variables, and user secrets do not configure the parameter.</param>
    /// <param name="description">Optional task help text describing the parameter to callers.</param>
    public static Param<T> Param<T>(string name, T defaultValue, string? description = null) where T : notnull =>
        new(name, ReadParam(name, defaultValue), description);

    /// <summary>Declares a string parameter whose resolved value is registered for log redaction.</summary>
    /// <param name="name">The non-empty configuration key, written as <c>--name value</c> on the command line or <c>DOTNETDO_name</c> in the environment.</param>
    /// <param name="defaultValue">The secret used when no higher-precedence source configures the parameter; <see langword="null"/> leaves it optional.</param>
    /// <param name="description">Optional task help text that must not reveal the secret value.</param>
    public static OptionalSecret Secret(string name, string? defaultValue = null, string? description = null) =>
        new(name, ReadSecret(name, defaultValue), description);

    static ParameterValue<T> ReadParam<T>(string name) =>
        ParameterConfiguration.Value.Read<T>(name) is { HasValue: true } value
            ? value
            : ParameterValue<T>.Missing(name);

    static ParameterValue<T> ReadParam<T>(string name, T defaultValue) =>
        ParameterConfiguration.Value.Read<T>(name) is { HasValue: true } value
            ? value
            : ParameterValue<T>.Resolved(name, defaultValue);

    static ParameterValue<string> ReadSecret(string name, string? defaultValue)
    {
        var value = ParameterConfiguration.Value.Read<string>(name) is { HasValue: true } configured
            ? configured.Value
            : defaultValue;

        return value is null
            ? ParameterValue<string>.Missing(name)
            : ParameterValue<string>.Resolved(name, value);
    }

    internal static string[] NormalizeParameterArguments(IEnumerable<string> arguments) =>
        TaskParameterConfiguration.NormalizeArguments(arguments);
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

    /// <summary>The configuration key used to resolve this parameter.</summary>
    public string Name { get; }
    /// <summary>Human-readable help text supplied by the task author.</summary>
    public string? Description { get; }
    /// <summary>The resolved parameter value.</summary>
    public T Value => _value.Value;

    /// <summary>Renders the resolved value as one quoted command-line argument.</summary>
    public string QuotedArgument() => Convert.ToString(_value.Value, CultureInfo.InvariantCulture)!.QuotedArgument();

    /// <summary>The resolved parameter value.</summary>
    /// <param name="parameter">The resolved parameter wrapper.</param>
    public static implicit operator T(Param<T> parameter) => parameter.Value;
}

/// <summary>An optional typed task parameter whose absence is represented by <see langword="null"/>.</summary>
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

    /// <summary>Resolves the optional parameter, prompting during an interactive local build or throwing when no value is available.</summary>
    public Param<T> Required() =>
        _value.HasValue ? new(Name, _value, Description)
        : ParameterPrompt.TryRead(Name, Description, secret: false, out T value) ? new(Name, ParameterValue<T>.Resolved(Name, value), Description)
        : throw new InvalidOperationException($"Parameter '{Name}' is required.");

    /// <summary>Renders the resolved optional value as one quoted command-line argument.</summary>
    public string? QuotedArgument() => _value.HasValue ? Convert.ToString(_value.Value, CultureInfo.InvariantCulture)?.QuotedArgument() : null;

    /// <summary>The resolved parameter value.</summary>
    /// <param name="parameter">The optional parameter wrapper.</param>
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

    /// <summary>Converts the optional parameter to its required form, prompting with masked input during an interactive local build or throwing when no value is available.</summary>
    public Secret Required() =>
        _value.HasValue ? new(Name, _value.Value, Description)
        : ParameterPrompt.TryRead(Name!, Description, secret: true, out string value) ? new(Name, value, Description)
        : throw new InvalidOperationException($"Secret parameter '{Name}' is required.");

    /// <inheritdoc />
    public override string ToString() => "***";
}

/// <summary>A secret with an available plaintext value.</summary>
public readonly record struct Secret
{
    readonly string _value;

    /// <summary>Wraps and registers a plaintext value for log redaction.</summary>
    /// <param name="value">The non-null plaintext value. It is registered for redaction, but callers must still avoid writing it outside a redacting logger.</param>
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

    /// <summary>The configuration key, or <see langword="null"/> when the secret was constructed directly.</summary>
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
