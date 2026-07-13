using System.Runtime.CompilerServices;
using System.Text;

namespace DotNetDo;

/// <summary>Base value object for rendering and awaiting a command-line tool invocation.</summary>
public abstract record ToolCommand
{
    readonly ArgumentSlots _arguments;

    /// <summary>Initializes command rendering state, cloning it when copied as a record.</summary>
    protected ToolCommand() => _arguments = new ArgumentSlots();

    /// <summary>Initializes command rendering state, cloning it when copied as a record.</summary>
    protected ToolCommand(ToolCommand original)
    {
        _arguments = original._arguments.Clone();
        AdditionalArguments = original.AdditionalArguments;
    }

    /// <summary>Unstructured arguments appended after typed options.</summary>
    public string? AdditionalArguments { get; init; }

    /// <summary>Gets or sets command prefix.</summary>
    protected abstract string CommandPrefix { get; }

    /// <summary>Set argument array.</summary>
    protected void SetArgumentArray(string key, string prefix, IReadOnlyCollection<string> values, string separator = " ")
    {
        ArgumentNullException.ThrowIfNull(values);
        SetArgument(key, prefix, values.Count == 0 ? null : string.Join(separator, values));
    }

    /// <summary>Get argument array.</summary>
    protected string[] GetArgumentArray(string key, string separator = " ")
    {
        var value = GetArgument(key);
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>Set flag.</summary>
    protected void SetFlag(string key, string flag, bool value) => SetArgument(key, value ? flag : null);

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
                });

    /// <summary>Get flag.</summary>
    protected bool? GetFlag(string key, string trueValue, string falseValue) =>
        GetArgument(key) switch
            {
                var value when value == trueValue => true,
                var value when value == falseValue => false,
                _ => null
            };

    /// <summary>Set enum.</summary>
    protected void SetEnum<T>(string key, T? value) where T : struct, Enum => SetArgument(key, value?.ToString().ToLowerInvariant());
    
    /// <summary>Get enum.</summary>
    protected T? GetEnum<T>(string key) where T : struct, Enum => Enum.TryParse<T>(GetArgument(key), true, out var value) ? value : null;

    /// <summary>Set argument.</summary>
    protected void SetArgument(string key, string? value) => SetArgument(key, "", value);

    /// <summary>Set argument.</summary>
    protected void SetArgument(string key, string prefix, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(prefix);

        if (string.IsNullOrWhiteSpace(value))
            _arguments.Set(key, "", "");
        else
            _arguments.Set(key, prefix, value);
    }

    /// <summary>Get argument.</summary>
    protected string? GetArgument(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _arguments.Get(key);
    }

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
            .Select(argument => argument.Prefix + argument.Value)
            .Where(argument => !string.IsNullOrWhiteSpace(argument));

        public string? Get(string key) => _arguments.FirstOrDefault(argument => argument.Key == key)?.Value;

        public void Set(string key, string prefix, string value)
        {
            var index = _arguments.FindIndex(argument => argument.Key == key);
            var argument = new Argument(key, prefix, value);

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

        sealed record Argument(string Key, string Prefix, string Value);
    }
}

/// <summary>A tool command whose await produces a semantic result.</summary>
public abstract record ToolCommand<TResult> : ToolCommand
{
    /// <summary>Allows awaiting the command's semantic execution.</summary>
    public TaskAwaiter<TResult> GetAwaiter() => ExecuteAsync().GetAwaiter();

    async Task<TResult> ExecuteAsync() => ReadResult(await ExecuteCommandAsync());

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
