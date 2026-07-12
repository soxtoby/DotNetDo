using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace DotNetDo;

public static partial class Do
{
    static readonly Lazy<IConfiguration> ParameterConfiguration = new(CreateParameterConfiguration);

    /// <summary>Declares a command-line parameter and resolves its configured value without executing user code during help discovery.</summary>
    public static Param<string> Param(string name) =>
        new(name, ReadParam<string>(name), null);

    /// <summary>Declares a command-line parameter and resolves its configured value without executing user code during help discovery.</summary>
    public static Param<string> Param(string name, string? defaultValue, string? description = null) =>
        new(name, defaultValue is null ? ReadParam<string>(name) : ReadParam(name, defaultValue), description);

    /// <summary>Declares a command-line parameter and resolves its configured value without executing user code during help discovery.</summary>
    public static Param<T> Param<T>(string name, string? description = null) =>
        new(name, ReadParam<T>(name), description);

    /// <summary>Declares a command-line parameter and resolves its configured value without executing user code during help discovery.</summary>
    public static Param<T> Param<T>(string name, T defaultValue, string? description = null) =>
        new(name, ReadParam<T>(name, defaultValue), description);

    /// <summary>Declares a string parameter whose resolved value is registered for log redaction.</summary>
    public static SecretParam Secret(string name) =>
        new(name, ReadSecret(name, null), null);

    /// <summary>Declares a string parameter whose resolved value is registered for log redaction.</summary>
    public static SecretParam Secret(string name, string? defaultValue, string? description = null) =>
        new(name, ReadSecret(name, defaultValue), description);

    static ParameterValue<T> ReadParam<T>(string name) =>
        ReadConfigurationValue<T>(name) is { HasValue: true } value
            ? value
            : ParameterValue<T>.Missing(name);

    static ParameterValue<T> ReadParam<T>(string name, T defaultValue) =>
        ReadConfigurationValue<T>(name) is { HasValue: true } value
            ? value
            : ParameterValue<T>.Resolved(name, defaultValue);

    static ParameterValue<Secret> ReadSecret(string name, string? defaultValue)
    {
        var value = ReadConfigurationValue<string>(name) is { HasValue: true } configured
            ? configured.Value
            : defaultValue;

        if (value is null)
            return ParameterValue<Secret>.Missing(name);

        SecretRedaction.Register(value);
        return ParameterValue<Secret>.Resolved(name, new Secret(value));
    }

    static ParameterValue<T> ReadConfigurationValue<T>(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var rawValue = ParameterConfiguration.Value[name];
        if (rawValue is null)
            return ParameterValue<T>.Missing(name);

        try
        {
            return ParameterValue<T>.Resolved(name, ParameterConfiguration.Value.GetValue<T>(name)!);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Parameter '{name}' could not be parsed as {typeof(T).Name}.", exception);
        }
    }

    static IConfiguration CreateParameterConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetEntryAssembly() ?? typeof(Do).Assembly, optional: true)
            .AddEnvironmentVariables("DOTNETDO_")
            .AddCommandLine(Environment.GetCommandLineArgs().Skip(1).ToArray());

        return builder.Build();
    }
}

/// <summary>An optional script parameter whose absence is represented by <see langword="null"/>.</summary>
public readonly record struct Param<T>
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
    /// <summary>Human-readable help text supplied by the script author.</summary>
    public string? Description { get; }
    /// <summary>The resolved parameter value.</summary>
    public T? Value => _value.ValueOrDefault;

    /// <summary>Converts the optional parameter to its required form, throwing when no value was supplied.</summary>
    public RequiredParam<T> Required() =>
        _value.HasValue
            ? new RequiredParam<T>(Name, _value.Value, Description)
            : throw new InvalidOperationException($"Parameter '{Name}' is required.");

    /// <summary>The resolved parameter value.</summary>
    public static implicit operator T?(Param<T> parameter) => parameter.Value;
}

/// <summary>A script parameter guaranteed to have resolved to a value.</summary>
public readonly record struct RequiredParam<T>
{
    internal RequiredParam(string name, T value, string? description)
    {
        Name = name;
        Value = value;
        Description = description;
    }

    /// <summary>The final path component, or <see langword="null"/> for a root or empty path.</summary>
    public string Name { get; }
    /// <summary>Human-readable help text supplied by the script author.</summary>
    public string? Description { get; }
    /// <summary>The resolved parameter value.</summary>
    public T Value { get; }

    /// <summary>T.</summary>
    public static implicit operator T(RequiredParam<T> parameter) => parameter.Value;
}

/// <summary>An optional string parameter that masks its value in text and logs.</summary>
public readonly record struct SecretParam
{
    readonly ParameterValue<Secret> _value;

    internal SecretParam(string name, ParameterValue<Secret> value, string? description)
    {
        Name = name;
        _value = value;
        Description = description;
    }

    /// <summary>The final path component, or <see langword="null"/> for a root or empty path.</summary>
    public string Name { get; }
    /// <summary>Human-readable help text supplied by the script author.</summary>
    public string? Description { get; }

    /// <summary>Returns the plaintext secret value; callers must avoid writing it to unredacted output.</summary>
    public string? Unwrap() => _value.HasValue ? _value.Value.Unwrap() : null;

    /// <summary>Converts the optional parameter to its required form, throwing when no value was supplied.</summary>
    public RequiredSecretParam Required() =>
        _value.HasValue
            ? new RequiredSecretParam(Name, _value.Value, Description)
            : throw new InvalidOperationException($"Secret parameter '{Name}' is required.");

    /// <inheritdoc />
    public override string ToString() => "***";
}

/// <summary>A required secret parameter with an available plaintext value.</summary>
public readonly record struct RequiredSecretParam
{
    readonly Secret _value;

    internal RequiredSecretParam(string name, Secret value, string? description)
    {
        Name = name;
        _value = value;
        Description = description;
    }

    /// <summary>The final path component, or <see langword="null"/> for a root or empty path.</summary>
    public string Name { get; }
    /// <summary>Human-readable help text supplied by the script author.</summary>
    public string? Description { get; }

    /// <summary>Returns the plaintext secret value; callers must avoid writing it to unredacted output.</summary>
    public string Unwrap() => _value.Unwrap();

    /// <inheritdoc />
    public override string ToString() => "***";
}

/// <summary>A plaintext secret wrapper that renders only a mask.</summary>
public readonly record struct Secret
{
    readonly string _value;

    internal Secret(string value) => _value = value;

    /// <summary>Returns the plaintext secret value; callers must avoid writing it to unredacted output.</summary>
    public string Unwrap() => _value;

    /// <inheritdoc />
    public override string ToString() => "***";
}

readonly record struct ParameterValue<T>(string Name, bool HasValue, T Value)
{
    public T? ValueOrDefault => HasValue ? Value : default;

    public static ParameterValue<T> Missing(string name) => new(name, false, default!);

    public static ParameterValue<T> Resolved(string name, T value) => new(name, true, value);
}
