using Serilog.Core;
using Serilog.Events;

namespace DotNetDo;

sealed class AzurePipelinesSink : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage();
        if (logEvent.Exception is not null)
            message += Environment.NewLine + logEvent.Exception;

        var output = logEvent.Level switch
            {
                LogEventLevel.Warning => $"##vso[task.logissue type=warning;]{Escape(message)}",
                LogEventLevel.Error or LogEventLevel.Fatal => $"##vso[task.logissue type=error;]{Escape(message)}",
                _ => message,
            };

        Console.WriteLine(output);
    }

    static string Escape(string value) => value
        .Replace("%", "%AZP25", StringComparison.Ordinal)
        .Replace("\r", "%0D", StringComparison.Ordinal)
        .Replace("\n", "%0A", StringComparison.Ordinal)
        .Replace(";", "%3B", StringComparison.Ordinal)
        .Replace("]", "%5D", StringComparison.Ordinal);
}