using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace DotNetDo.Tests;

[Collection("Global logger")]
public sealed class ExecLoggingTests
{
    [Fact]
    public async Task Successful_command_logs_start_but_not_completion()
    {
        var previous = Log.Logger;
        var sink = new CapturingSink();
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Sink(sink)
            .CreateLogger();

        try
        {
            Log.Logger = logger;

            await Do.Exec("dotnet --version");

            Assert.Contains(sink.Events, @event => @event.MessageTemplate.Text.StartsWith("Executing "));
            Assert.DoesNotContain(sink.Events, @event => @event.MessageTemplate.Text.Contains("completed successfully"));
        }
        finally
        {
            Log.Logger = previous;
        }
    }

    [Fact]
    public void Default_log_uses_escaped_output_as_the_message_template()
    {
        var previous = Log.Logger;
        var sink = new CapturingSink();
        using var logger = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .CreateLogger();

        try
        {
            Log.Logger = logger;

            ExecOptions.DefaultLog(OutputType.Out, "Built {Project}");

            var @event = Assert.Single(sink.Events);
            Assert.Equal("Built {{Project}}", @event.MessageTemplate.Text);
            Assert.Equal("Built {Project}", @event.RenderMessage());
            Assert.Empty(@event.Properties);
        }
        finally
        {
            Log.Logger = previous;
        }
    }

    sealed class CapturingSink : ILogEventSink
    {
        readonly List<LogEvent> _events = [];

        public LogEvent[] Events
        {
            get
            {
                lock (_events)
                    return [.. _events];
            }
        }

        public void Emit(LogEvent logEvent)
        {
            lock (_events)
                _events.Add(logEvent);
        }
    }
}

[CollectionDefinition("Global logger", DisableParallelization = true)]
public sealed class GlobalLoggerCollection;
