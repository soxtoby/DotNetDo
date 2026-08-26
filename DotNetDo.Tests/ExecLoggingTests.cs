using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Collections.Immutable;
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

            ExecLog.Default.Write(OutputType.Out, "Built {Project}");

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

    [Fact]
    public void None_does_not_log_output()
    {
        var previous = Log.Logger;
        var sink = new CapturingSink();
        using var logger = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .CreateLogger();

        try
        {
            Log.Logger = logger;

            ExecLog.None.Write(OutputType.Out, "ordinary output");
            ExecLog.None.Write(OutputType.Error, "error output");

            Assert.Empty(sink.Events);
        }
        finally
        {
            Log.Logger = previous;
        }
    }

    [Fact]
    public void Errors_only_logs_standard_error_with_default_behavior()
    {
        var previous = Log.Logger;
        var sink = new CapturingSink();
        using var logger = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .CreateLogger();

        try
        {
            Log.Logger = logger;

            ExecLog.ErrorsOnly.Write(OutputType.Out, "ordinary output");
            ExecLog.ErrorsOnly.Write(OutputType.Error, "Error {Code}");

            var @event = Assert.Single(sink.Events);
            Assert.Equal(LogEventLevel.Error, @event.Level);
            Assert.Equal("Error {Code}", @event.RenderMessage());
            Assert.Empty(@event.Properties);
        }
        finally
        {
            Log.Logger = previous;
        }
    }

    [Fact]
    public void Filter_forwards_only_matching_output_to_the_wrapped_log()
    {
        var output = new List<(OutputType Type, string Message)>();
        var log = new ExecLog((type, message) => output.Add((type, message)))
            .Filter((_, message) => message.StartsWith("keep", StringComparison.Ordinal));

        log.Write(OutputType.Out, "drop this");
        log.Write(OutputType.Error, "keep this");

        Assert.Equal([(OutputType.Error, "keep this")], output);
    }

    [Fact]
    public async Task Process_environment_is_not_logged()
    {
        const string name = "DOTNETDO_TEST_LOG_ENVIRONMENT";
        const string value = "environment-secret-value";
        var previous = Log.Logger;
        var sink = new CapturingSink();
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Sink(sink)
            .CreateLogger();

        try
        {
            Log.Logger = logger;

            await Do.Exec(
                "dotnet --version",
                new ExecOptions
                    {
                        Environment = environment => environment.SetItem(name, value),
                        Log = ExecLog.None,
                    });

            Assert.DoesNotContain(sink.Events, @event =>
                @event.RenderMessage().Contains(name, StringComparison.Ordinal)
                || @event.RenderMessage().Contains(value, StringComparison.Ordinal));
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
