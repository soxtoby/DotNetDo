using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Core;

namespace DotNetDo;

static class LoggingBootstrap
{
    static ILogger? _bootstrapLogger;

    [ModuleInitializer]
    [SuppressMessage(
        "Usage",
        "CA2255:The ModuleInitializer attribute should not be used in libraries",
        Justification = "Bootstrapping the logger before task code runs is a core feature."
    )]
    internal static void Initialize()
    {
        if (!ReferenceEquals(Log.Logger, Logger.None))
            return;

        var logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(Logging.LevelSwitch)
            .WriteTo.DefaultOutput()
            .CreateRedactingLogger();

        _bootstrapLogger = logger;
        Log.Logger = logger;
        AppDomain.CurrentDomain.ProcessExit += (_, _) => DisposeBootstrapLogger();
    }

    static void DisposeBootstrapLogger()
    {
        var logger = Interlocked.Exchange(ref _bootstrapLogger, null);
        (logger as IDisposable)?.Dispose();
    }
}