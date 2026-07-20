using System.Globalization;
using System.Runtime.CompilerServices;

namespace DotNetDo;

/// <summary>Base value object for rendering and awaiting a command-line tool invocation.</summary>
public abstract record ToolCommand : ExecOptions
{
    /// <summary>Unstructured arguments appended after typed options.</summary>
    public string? AdditionalArguments { get; init; }

    /// <summary>Gets every canonically ordered rendered command fragment.</summary>
    protected abstract IReadOnlyList<string?> CommandParts { get; }

    /// <summary>Renders an argument when enabled.</summary>
    protected static string? Arg(string argument, bool value) => value ? argument : null;

    /// <summary>Renders one of two arguments for a nullable choice.</summary>
    protected static string? Arg(string trueValue, string falseValue, bool? value) =>
        value switch
            {
                true => trueValue,
                false => falseValue,
                null => null,
            };

    /// <summary>Renders one typed positional semantic value.</summary>
    protected static string? Arg<T>(T? value, bool quote = true) => Arg("", value, quote);

    /// <summary>Renders one typed semantic value.</summary>
    protected static string? Arg<T>(string prefix, T? value, bool quote = true)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        var rendered = Convert(value);
        return string.IsNullOrWhiteSpace(rendered)
            ? null
            : RenderPrefix(prefix) + (quote ? rendered.QuotedArgument() : rendered);
    }

    /// <summary>Renders typed semantic values independently, then joins them.</summary>
    protected static string? Args<T>(string prefix, IEnumerable<T> values, string separator = " ", bool quote = true)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(separator);

        var rendered = values
            .Select(Convert)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => quote ? value!.QuotedArgument() : value!)
            .ToArray();

        return rendered.Length == 0
            ? null
            : RenderPrefix(prefix) + string.Join(separator, rendered);
    }

    /// <summary>Renders typed positional semantic values independently, then joins them.</summary>
    protected static string? Args<T>(IEnumerable<T> values, string separator = " ", bool quote = true) =>
        Args("", values, separator, quote);

    /// <inheritdoc />
    public sealed override string ToString()
    {
        var commandParts = CommandParts;
        ArgumentNullException.ThrowIfNull(commandParts);
        var parts = commandParts
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim())
            .ToArray();

        if (parts.Length == 0)
            throw new InvalidOperationException("A tool command must contain at least one command part.");

        return string.Join(
            " ",
            parts
                .Append(AdditionalArguments)
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim()));
    }

    static string? Convert<T>(T? value) =>
        value switch
            {
                null => null,
                Enum enumValue => enumValue.ToString().ToLowerInvariant(),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };

    static string RenderPrefix(string prefix)
    {
        if (prefix.Length != prefix.Trim().Length)
            throw new ArgumentException("Argument prefixes cannot contain leading or trailing whitespace.", nameof(prefix));
        return prefix.Length == 0 || prefix[^1] is ':' or '=' or ',' ? prefix : prefix + " ";
    }
}

/// <summary>A tool command whose await produces a semantic result.</summary>
public abstract record ToolCommand<TResult> : ToolCommand
{
    /// <summary>Allows awaiting the command's semantic execution.</summary>
    public TaskAwaiter<TResult> GetAwaiter() => ExecuteAsync().GetAwaiter();

    async Task<TResult> ExecuteAsync()
    {
        var result = await ExecuteCommandAsync();

        try
        {
            return ReadResult(result);
        }
        catch (ToolOutputException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ToolOutputException(result, typeof(TResult), exception);
        }
    }

    /// <summary>Executes the rendered command and returns its successful raw process result.</summary>
    protected virtual async Task<ExecResult> ExecuteCommandAsync() => await Do.Exec(this);

    /// <summary>Converts a successful raw process result to the command's semantic result.</summary>
    protected abstract TResult ReadResult(ExecResult result);
}

/// <summary>A process-backed tool command whose await produces its successful raw process result.</summary>
public abstract record ExecToolCommand : ToolCommand<ExecResult>
{
    /// <inheritdoc />
    protected override ExecResult ReadResult(ExecResult result) => result;
}

/// <summary>Indicates that successful raw tool output could not be converted to its semantic result.</summary>
public sealed class ToolOutputException(ExecResult result, Type expectedType, Exception innerException)
    : Exception($"Command '{result.Command}' produced output that could not be read as {expectedType.Name}.", innerException)
{
    /// <summary>The raw successful process result, retained for inspection.</summary>
    public ExecResult Result { get; } = result;

    /// <summary>The semantic result type expected by the tool command.</summary>
    public Type ExpectedType { get; } = expectedType;
}
