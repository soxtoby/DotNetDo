using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace DotNetDo.Tests;

[Collection("Global logger")]
public sealed class ExecLoggingTests
{
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

            Assert.Equal("Built {{Project}}", sink.Event!.MessageTemplate.Text);
            Assert.Equal("Built {Project}", sink.Event.RenderMessage());
            Assert.Empty(sink.Event.Properties);
        }
        finally
        {
            Log.Logger = previous;
        }
    }

    sealed class CapturingSink : ILogEventSink
    {
        public LogEvent? Event { get; private set; }

        public void Emit(LogEvent logEvent) => Event = logEvent;
    }
}

[CollectionDefinition("Global logger", DisableParallelization = true)]
public sealed class GlobalLoggerCollection;
