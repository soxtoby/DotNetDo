using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace DotNetDo;

sealed class TaskParameterConfiguration
{
    const string ImplicitBooleanValue = "\0";
    readonly IConfiguration _configuration;

    TaskParameterConfiguration(IConfiguration configuration) => _configuration = configuration;

    public string? this[string name] => _configuration[name];

    public static TaskParameterConfiguration Current { get; } = Create(
        Environment.GetCommandLineArgs().Skip(1),
        Assembly.GetEntryAssembly() ?? typeof(Do).Assembly);

    public static TaskParameterConfiguration Create(IEnumerable<string> arguments, string? userSecretsId = null)
    {
        var builder = new ConfigurationBuilder();
        if (userSecretsId is not null)
            builder.AddUserSecrets(userSecretsId, reloadOnChange: false);
        return Build(builder, arguments);
    }

    static TaskParameterConfiguration Create(IEnumerable<string> arguments, Assembly userSecretsAssembly) =>
        Build(new ConfigurationBuilder().AddUserSecrets(userSecretsAssembly, optional: true), arguments);

    static TaskParameterConfiguration Build(IConfigurationBuilder builder, IEnumerable<string> arguments) =>
        new(builder
            .AddEnvironmentVariables("DOTNETDO_")
            .AddCommandLine(NormalizeArguments(arguments))
            .Build());

    public ParameterValue<T> Read<T>(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        switch (_configuration[name])
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
                    return ParameterValue<T>.Resolved(name, _configuration.GetValue<T>(name)!);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"Parameter '{name}' could not be parsed as {typeof(T).Name}.", exception);
                }
        }
    }

    public static string[] NormalizeArguments(IEnumerable<string> arguments)
    {
        var values = arguments.TakeWhile(argument => argument != "--").ToArray();
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
