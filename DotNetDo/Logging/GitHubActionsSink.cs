using Serilog.Core;
using Serilog.Events;

namespace DotNetDo;

sealed class GitHubActionsSink : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage();
        if (logEvent.Exception is not null)
            message += Environment.NewLine + logEvent.Exception;

        var output = logEvent.Level switch
            {
                LogEventLevel.Warning => $"::warning::{Escape(message)}",
                LogEventLevel.Error or LogEventLevel.Fatal => $"::error::{Escape(message)}",
                _ => message,
            };

        Console.WriteLine(output);
    }

    static string Escape(string value) => value
        .Replace("%", "%25", StringComparison.Ordinal)
        .Replace("\r", "%0D", StringComparison.Ordinal)
        .Replace("\n", "%0A", StringComparison.Ordinal);
}