using Serilog.Core;
using Serilog.Events;

namespace DotNetDo;

sealed class CISink : ILogEventSink
{
    readonly GitHubActions? _gitHubActions;
    readonly AzurePipelines? _azurePipelines;

    public CISink() : this(Do.GitHubActions, Do.AzurePipelines) { }

    internal CISink(GitHubActions? gitHubActions, AzurePipelines? azurePipelines)
    {
        _gitHubActions = gitHubActions;
        _azurePipelines = azurePipelines;
    }

    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage();
        if (logEvent.Exception is not null)
            message += Environment.NewLine + logEvent.Exception;

        switch (logEvent.Level)
        {
            case LogEventLevel.Verbose or LogEventLevel.Debug:
                _gitHubActions?.Debug(message);
                _azurePipelines?.Debug(message);
                return;
            case LogEventLevel.Warning:
                _gitHubActions?.Warning(message);
                _azurePipelines?.LogIssue(message, new() { Type = AzureIssueType.Warning });
                return;
            case LogEventLevel.Error or LogEventLevel.Fatal:
                _gitHubActions?.Error(message);
                _azurePipelines?.LogIssue(message, new() { Type = AzureIssueType.Error });
                return;
            default:
                Console.WriteLine(message);
                break;
        }
    }
}
