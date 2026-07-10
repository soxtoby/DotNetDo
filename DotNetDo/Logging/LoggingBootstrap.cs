using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Core;

namespace DotNetDo;

static class LoggingBootstrap
{
    static ILogger? _bootstrapLogger;

    [ModuleInitializer]
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