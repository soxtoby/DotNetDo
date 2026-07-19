using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace DotNetDo;

/// <summary>Base value object for rendering and awaiting a command-line tool invocation.</summary>
public abstract record ToolCommand : ExecOptions
{
    readonly ArgumentSlots _arguments;

    /// <summary>Initializes command rendering state, cloning it when copied as a record.</summary>
    protected ToolCommand() => _arguments = new ArgumentSlots();

    /// <summary>Initializes command rendering state, cloning it when copied as a record.</summary>
    protected ToolCommand(ToolCommand original) : base(original)
    {
        _arguments = original._arguments.Clone();
        AdditionalArguments = original.AdditionalArguments;
    }

    /// <summary>Unstructured arguments appended after typed options.</summary>
    public string? AdditionalArguments { get; init; }

    /// <summary>Gets or sets command prefix.</summary>
    protected abstract string CommandPrefix { get; }

    /// <summary>Stores semantic argument values, quoting each during rendering unless disabled.</summary>
    protected void SetArgumentArray(string key, string prefix, IReadOnlyCollection<string> values, string separator = " ", bool quote = true)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(separator);

        _arguments.Set(key, prefix, values.ToArray(), separator, quote);
    }

    /// <summary>Returns a snapshot of the semantic values stored for an argument slot.</summary>
    protected IReadOnlyList<string> GetArgumentArray(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _arguments.GetValues(key) ?? [];
    }

    /// <summary>Set flag.</summary>
    protected void SetFlag(string key, string flag, bool value) => SetArgument(key, value ? flag : null, quote: false);

    /// <summary>Get flag.</summary>
    protected bool GetFlag(string key) => GetArgument(key) is not null;

    /// <summary>Set flag.</summary>
    protected void SetFlag(string key, string trueValue, string falseValue, bool? value) =>
        SetArgument(
            key,
            value switch
                {
                    true => trueValue,
                    false => falseValue,
                    null => null
                },
            quote: false);

    /// <summary>Get flag.</summary>
    protected bool? GetFlag(string key, string trueValue, string falseValue) =>
        GetArgument(key) switch
            {
                var value when value == trueValue => true,
                var value when value == falseValue => false,
                _ => null
            };

    /// <summary>Set enum.</summary>
    protected void SetEnum<T>(string key, T? value) where T : struct, Enum => SetEnum(key, "", value);

    /// <summary>Set prefixed enum.</summary>
    protected void SetEnum<T>(string key, string prefix, T? value) where T : struct, Enum =>
        SetArgument(key, prefix, value?.ToString().ToLowerInvariant(), quote: false);
    
    /// <summary>Get enum.</summary>
    protected T? GetEnum<T>(string key) where T : struct, Enum => Enum.TryParse<T>(GetArgument(key), true, out var value) ? value : null;

    /// <summary>Stores a semantic argument value, quoting it during rendering unless disabled.</summary>
    protected void SetArgument(string key, string? value, bool quote = true) => SetArgument(key, "", value, quote);

    /// <summary>Stores a prefixed semantic argument value, quoting it during rendering unless disabled.</summary>
    protected void SetArgument(string key, string prefix, string? value, bool quote = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(prefix);
        _arguments.Set(key, prefix, value is null ? null : [value], " ", quote);
    }

    /// <summary>Returns the exact semantic value stored for an argument slot.</summary>
    protected string? GetArgument(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _arguments.Get(key);
    }

    /// <summary>Stores an integer argument using invariant formatting.</summary>
    protected void SetInt(string key, string prefix, int? value) =>
        SetArgument(key, prefix, value?.ToString(CultureInfo.InvariantCulture), quote: false);

    /// <summary>Returns an integer argument parsed using invariant formatting.</summary>
    protected int? GetInt(string key) =>
        int.TryParse(GetArgument(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    /// <inheritdoc />
    public sealed override string ToString()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(CommandPrefix);

        var command = new StringBuilder(CommandPrefix.Trim());

        foreach (var argument in _arguments.All)
            Append(command, argument);

        if (!string.IsNullOrWhiteSpace(AdditionalArguments))
            Append(command, AdditionalArguments.Trim());

        return command.ToString();
    }

    static void Append(StringBuilder command, string argument)
    {
        command.Append(' ');
        command.Append(argument);
    }

    sealed class ArgumentSlots
    {
        readonly List<Argument> _arguments = [];

        public IEnumerable<string> All => _arguments
            .Select(Render)
            .Where(argument => !string.IsNullOrWhiteSpace(argument));

        public string? Get(string key) => _arguments.FirstOrDefault(argument => argument.Key == key)?.Values?.FirstOrDefault();

        public IReadOnlyList<string>? GetValues(string key) => _arguments.FirstOrDefault(argument => argument.Key == key)?.Values;

        public void Set(string key, string prefix, string[]? values, string separator, bool quote)
        {
            var index = _arguments.FindIndex(argument => argument.Key == key);
            var argument = new Argument(key, prefix, values, separator, quote);

            if (index >= 0)
                _arguments[index] = argument;
            else
                _arguments.Add(argument);
        }

        public ArgumentSlots Clone()
        {
            var clone = new ArgumentSlots();
            clone._arguments.AddRange(_arguments);
            return clone;
        }

        static string Render(Argument argument)
        {
            var values = argument.Values?
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => argument.Quote ? value.QuotedArgument() : value)
                .ToArray() ?? [];

            return values.Length == 0
                ? ""
                : argument.Prefix + string.Join(argument.Separator, values);
        }

        sealed record Argument(string Key, string Prefix, string[]? Values, string Separator, bool Quote);
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
