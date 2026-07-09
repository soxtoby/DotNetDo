using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace DotNetDo;

public static partial class Do
{
    static readonly Lazy<IConfiguration> ParameterConfiguration = new(CreateParameterConfiguration);

    public static Param<string> Param(string name) =>
        new(name, ReadParam<string>(name), null);

    public static Param<string> Param(string name, string? defaultValue, string? description = null) =>
        new(name, defaultValue is null ? ReadParam<string>(name) : ReadParam(name, defaultValue), description);

    public static Param<T> Param<T>(string name, string? description = null) =>
        new(name, ReadParam<T>(name), description);

    public static Param<T> Param<T>(string name, T defaultValue, string? description = null) =>
        new(name, ReadParam<T>(name, defaultValue), description);

    public static SecretParam Secret(string name) =>
        new(name, ReadSecret(name, null), null);

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

    static ParameterValue<Secret> ReadSecret(string name, string? defaultValue) =>
        ReadConfigurationValue<string>(name) is { HasValue: true } value
            ? ParameterValue<Secret>.Resolved(name, new Secret(value.Value))
            : defaultValue is null
                ? ParameterValue<Secret>.Missing(name)
                : ParameterValue<Secret>.Resolved(name, new Secret(defaultValue));

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
            .AddCommandLine(Args);

        return builder.Build();
    }
}

public readonly record struct Param<T>
{
    readonly ParameterValue<T> _value;

    internal Param(string name, ParameterValue<T> value, string? description)
    {
        Name = name;
        _value = value;
        Description = description;
    }

    public string Name { get; }
    public string? Description { get; }
    public T? Value => _value.ValueOrDefault;

    public RequiredParam<T> Required() =>
        _value.HasValue
            ? new RequiredParam<T>(Name, _value.Value, Description)
            : throw new InvalidOperationException($"Parameter '{Name}' is required.");

    public static implicit operator T?(Param<T> parameter) => parameter.Value;
}

public readonly record struct RequiredParam<T>
{
    internal RequiredParam(string name, T value, string? description)
    {
        Name = name;
        Value = value;
        Description = description;
    }

    public string Name { get; }
    public string? Description { get; }
    public T Value { get; }

    public static implicit operator T(RequiredParam<T> parameter) => parameter.Value;
}

public readonly record struct SecretParam
{
    readonly ParameterValue<Secret> _value;

    internal SecretParam(string name, ParameterValue<Secret> value, string? description)
    {
        Name = name;
        _value = value;
        Description = description;
    }

    public string Name { get; }
    public string? Description { get; }

    public string? Unwrap() => _value.HasValue ? _value.Value.Unwrap() : null;

    public RequiredSecretParam Required() =>
        _value.HasValue
            ? new RequiredSecretParam(Name, _value.Value, Description)
            : throw new InvalidOperationException($"Secret parameter '{Name}' is required.");

    public override string ToString() => "***";
}

public readonly record struct RequiredSecretParam
{
    readonly Secret _value;

    internal RequiredSecretParam(string name, Secret value, string? description)
    {
        Name = name;
        _value = value;
        Description = description;
    }

    public string Name { get; }
    public string? Description { get; }

    public string Unwrap() => _value.Unwrap();

    public override string ToString() => "***";
}

public readonly record struct Secret
{
    readonly string _value;

    internal Secret(string value) => _value = value;

    public string Unwrap() => _value;

    public override string ToString() => "***";
}

readonly record struct ParameterValue<T>(string Name, bool HasValue, T Value)
{
    public T? ValueOrDefault => HasValue ? Value : default;

    public static ParameterValue<T> Missing(string name) => new(name, false, default!);

    public static ParameterValue<T> Resolved(string name, T value) => new(name, true, value);
}
