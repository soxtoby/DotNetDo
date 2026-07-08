using System.Text;

namespace DotNetDo;

public abstract record ToolCommandConfig
{
    readonly ArgumentSlots _arguments;

    protected ToolCommandConfig()
    {
        _arguments = new ArgumentSlots();
    }

    protected ToolCommandConfig(ToolCommandConfig original)
    {
        _arguments = original._arguments.Clone();
        AdditionalArguments = original.AdditionalArguments;
    }

    public string? AdditionalArguments { get; init; }

    protected abstract string CommandPrefix { get; }

    protected void SetArgumentArray(string key, string prefix, IReadOnlyCollection<string> values, string separator = " ")
    {
        ArgumentNullException.ThrowIfNull(values);
        SetArgument(key, prefix, values.Count == 0 ? null : string.Join(separator, values));
    }

    protected string[] GetArgumentArray(string key, string separator = " ")
    {
        var value = GetArgument(key);
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    protected void SetFlag(string key, string flag, bool value) => SetArgument(key, value ? flag : null);

    protected bool GetFlag(string key) => GetArgument(key) is not null;

    protected void SetFlag(string key, string trueValue, string falseValue, bool? value) =>
        SetArgument(
            key,
            value switch
                {
                    true => trueValue,
                    false => falseValue,
                    null => null
                });

    protected bool? GetFlag(string key, string trueValue, string falseValue) =>
        GetArgument(key) switch
            {
                var value when value == trueValue => true,
                var value when value == falseValue => false,
                _ => null
            };

    protected void SetEnum<T>(string key, T? value) where T : struct, Enum => SetArgument(key, value?.ToString().ToLowerInvariant());
    
    protected T? GetEnum<T>(string key) where T : struct, Enum => Enum.TryParse<T>(GetArgument(key), true, out var value) ? value : null;

    protected void SetArgument(string key, string? value) => SetArgument(key, "", value);

    protected void SetArgument(string key, string prefix, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(prefix);

        if (string.IsNullOrWhiteSpace(value))
            _arguments.Set(key, "", "");
        else
            _arguments.Set(key, prefix, value);
    }

    protected string? GetArgument(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _arguments.Get(key);
    }

    public override string ToString()
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
