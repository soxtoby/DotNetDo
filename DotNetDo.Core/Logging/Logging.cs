using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace DotNetDo;

/// <summary>Configures DotNetDo's environment-aware Serilog output.</summary>
public static class Logging
{
    internal static readonly LoggingLevelSwitch LevelSwitch = new();

    /// <summary>
    /// Gets or sets the minimum log event level. The default is <see cref="LogEventLevel.Information"/>.
    /// </summary>
    public static LogEventLevel Level { get => LevelSwitch.MinimumLevel; set => LevelSwitch.MinimumLevel = value; }

    /// <summary>
    /// Writes log events to the default output sink, which is determined by the environment.
    /// Supported CI providers receive native annotations; otherwise output uses the console sink.
    /// Otherwise, it will write to the console.
    /// </summary>
    public static LoggerConfiguration DefaultOutput(this LoggerSinkConfiguration sinkConfiguration)
    {
        ArgumentNullException.ThrowIfNull(sinkConfiguration);

        return Do.GitHubActions is not null || Do.AzurePipelines is not null
            ? sinkConfiguration.Sink(new CISink())
            : sinkConfiguration.Console();
    }

    /// <summary>
    /// Redacts <see cref="SecretParam"/> values from log events.
    /// </summary>
    public static ILogger CreateRedactingLogger(this LoggerConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var configuredLogger = configuration.CreateLogger();
        return new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(new RedactingSink(configuredLogger))
            .CreateLogger();
    }
}
