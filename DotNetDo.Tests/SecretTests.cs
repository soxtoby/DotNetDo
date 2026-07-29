using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace DotNetDo.Tests;

public sealed class SecretTests
{
    [Fact]
    public void Direct_secret_construction_registers_value_for_redaction()
    {
        var value = $"secret-{Guid.NewGuid()}";
        var secret = new Secret(value);
        var sink = new CapturingSink();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .CreateRedactingLogger();
        try
        {
            logger.Information("Value: {Value}", value);

            Assert.Equal(value, secret.Unwrap());
            Assert.Equal("***", secret.ToString());
            Assert.DoesNotContain(value, sink.Event!.ToString());
        }
        finally
        {
            (logger as IDisposable)?.Dispose();
        }
    }

    [Fact]
    public void Do_secret_returns_optional_secret()
    {
        OptionalSecret secret = Do.Secret($"secret-{Guid.NewGuid()}", "value");

        Assert.Equal("value", secret.Unwrap());
    }

    [Fact]
    public void Required_secret_has_non_nullable_value()
    {
        Secret secret = Do.Secret($"secret-{Guid.NewGuid()}", "value").Required();
        string value = secret.Unwrap();

        Assert.Equal("value", value);
    }

    sealed class CapturingSink : ILogEventSink
    {
        public LogEvent? Event { get; private set; }

        public void Emit(LogEvent logEvent) => Event = logEvent;
    }
}
