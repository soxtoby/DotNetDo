using Serilog.Events;

namespace DotNetDo;

/// <summary>Controls how process output lines are logged.</summary>
public sealed class ExecLog
{
    readonly Action<OutputType, string> _write;

    /// <summary>Creates process-output logging with custom per-line behavior.</summary>
    public ExecLog(Action<OutputType, string> write)
    {
        ArgumentNullException.ThrowIfNull(write);
        _write = write;
    }

    /// <summary>Logs standard output at Information and standard error at Error through the global Serilog logger.</summary>
    public static ExecLog Default { get; } = new(DefaultWrite);

    /// <summary>Does not log process output.</summary>
    public static ExecLog None { get; } = new(static (_, _) => { });

    /// <summary>Logs only standard-error output, using the default logging behavior.</summary>
    public static ExecLog ErrorsOnly { get; } = Default.Filter(static (type, _) => type == OutputType.Error);

    /// <summary>Returns logging that forwards matching output to this logging behavior.</summary>
    public ExecLog Filter(Func<OutputType, string, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return new((type, message) =>
            {
                if (predicate(type, message))
                    Write(type, message);
            });
    }

    internal void Write(OutputType type, string message) => _write(type, message);

    static void DefaultWrite(OutputType type, string message) =>
        Serilog.Log.Write(
            type == OutputType.Out ? LogEventLevel.Information : LogEventLevel.Error,
            message
                .Replace("{", "{{", StringComparison.Ordinal)
                .Replace("}", "}}", StringComparison.Ordinal));
}
