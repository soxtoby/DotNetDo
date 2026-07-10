using System.Security;
using System.Text.Encodings.Web;
using System.Text.Json;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace DotNetDo;

static class SecretRedaction
{
    static readonly Lock Gate = new();
    static string[] _targets = [];

    public static void Register(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        var variants = new[]
            {
                value,
                value.QuotedArgument(),
                JsonSerializer.Serialize(value),
                SecurityElement.Escape(value),
                JavaScriptEncoder.Default.Encode(value),
                Uri.EscapeDataString(value)
            };

        lock (Gate)
        {
            var targets = _targets
                .Concat(variants)
                .Where(target => target.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(target => target.Length)
                .ToArray();

            Volatile.Write(ref _targets, targets);
        }
    }

    public static string Redact(string value)
    {
        var redacted = value;
        foreach (var target in Volatile.Read(ref _targets))
            redacted = redacted.Replace(target, "***", StringComparison.Ordinal);
        return redacted;
    }

    public static bool RequiresRedaction(string value) => Volatile.Read(ref _targets).Any(target => value.Contains(target, StringComparison.Ordinal));
}

sealed class RedactingSink(ILogEventSink inner) : ILogEventSink, IDisposable, IAsyncDisposable
{
    long _nextRedaction;

    public void Emit(LogEvent logEvent)
    {
        if (RequiresRedaction(logEvent))
        {
            var exception = RedactException(logEvent.Exception);
            var messageTemplate = RedactMessageTemplate(logEvent.MessageTemplate);
            var properties = logEvent.Properties
                .Select(property => new LogEventProperty(
                    RedactName(property.Key),
                    RedactValue(property.Value)))
                .ToList();

            inner.Emit(new LogEvent(
                logEvent.Timestamp,
                logEvent.Level,
                exception,
                messageTemplate,
                properties,
                logEvent.TraceId ?? default,
                logEvent.SpanId ?? default));
        }
        else
        {
            inner.Emit(logEvent);
        }
    }

    static bool RequiresRedaction(LogEvent logEvent) =>
        SecretRedaction.RequiresRedaction(logEvent.MessageTemplate.Text)
        || logEvent.Exception is not null && SecretRedaction.RequiresRedaction(logEvent.Exception.ToString())
        || logEvent.Properties.Any(property =>
            SecretRedaction.RequiresRedaction(property.Key)
            || SecretRedaction.RequiresRedaction(property.Value.ToString()));

    static MessageTemplate RedactMessageTemplate(MessageTemplate messageTemplate)
    {
        var redacted = SecretRedaction.Redact(messageTemplate.Text);
        if (ReferenceEquals(redacted, messageTemplate.Text))
            return messageTemplate;

        return new MessageTemplate(
            redacted.Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal),
                [new TextToken(redacted)]);
    }

    static RedactedException? RedactException(Exception? exception) =>
        exception is null ? null : new RedactedException(SecretRedaction.Redact(exception.ToString()));

    LogEventPropertyValue RedactValue(LogEventPropertyValue value) =>
        value switch
            {
                DictionaryValue dictionary => new DictionaryValue(dictionary.Elements
                    .Select(element => KeyValuePair.Create(RedactScalar(element.Key), RedactValue(element.Value)))),
                ScalarValue scalar => RedactScalar(scalar),
                SequenceValue sequence => new SequenceValue(sequence.Elements.Select(RedactValue)),
                StructureValue structure => new StructureValue(
                    structure.Properties
                        .Select(property => new LogEventProperty(
                            RedactName(property.Name),
                            RedactValue(property.Value))),
                    structure.TypeTag is null ? null : SecretRedaction.Redact(structure.TypeTag)),
                _ => RedactScalar(new ScalarValue(value.ToString())),
            };

    static ScalarValue RedactScalar(ScalarValue scalar)
    {
        var value = scalar.Value as string ?? scalar.ToString();
        return SecretRedaction.RequiresRedaction(value)
            ? new ScalarValue(SecretRedaction.Redact(value))
            : scalar;
    }

    string RedactName(string name)
    {
        if (!SecretRedaction.RequiresRedaction(name))
            return name;

        return $"Redacted{Interlocked.Increment(ref _nextRedaction)}";
    }

    public void Dispose() => (inner as IDisposable)?.Dispose();

    public async ValueTask DisposeAsync()
    {
        if (inner is IAsyncDisposable disposable)
            await disposable.DisposeAsync();
        else
            Dispose();
    }

    sealed class RedactedException(string value) : Exception("Exception details were redacted; use ToString().")
    {
        public override string? StackTrace => null;
        public override string? Source => null;
        public override string ToString() => value;
    }
}
