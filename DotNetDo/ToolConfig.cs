using System.Text;

namespace DotNetDo;

public abstract class ToolConfig<TSelf>
    where TSelf : ToolConfig<TSelf>
{
    readonly List<Argument> _arguments = [];
    string _additionalArguments = "";

    internal ExecProcess Exec(Action<TSelf>? configure = null, ExecOptions? options = null)
    {
        configure?.Invoke(Self);
        return Do.Exec(BuildCommand(), options);
    }
    
    protected abstract string CommandPrefix { get; }

    protected TSelf WithArgument(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var index = _arguments.FindIndex(argument => argument.Key == key);
        var argument = new Argument(key, value);

        if (index >= 0)
            _arguments[index] = argument;
        else
            _arguments.Add(argument);

        return Self;
    }

    protected TSelf WithoutArgument(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var index = _arguments.FindIndex(argument => argument.Key == key);
        if (index >= 0)
            _arguments[index] = _arguments[index] with { Value = "" };

        return Self;
    }

    protected TSelf WithAdditionalArguments(string arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        _additionalArguments = arguments;
        return Self;
    }

    protected TSelf WithoutAdditionalArguments()
    {
        _additionalArguments = "";
        return Self;
    }

    string BuildCommand()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(CommandPrefix);

        var command = new StringBuilder(CommandPrefix.Trim());

        foreach (var argument in _arguments.Where(argument => !string.IsNullOrWhiteSpace(argument.Value)))
            Append(command, argument.Value);

        if (!string.IsNullOrWhiteSpace(_additionalArguments))
            Append(command, _additionalArguments.Trim());

        return command.ToString();
    }

    static void Append(StringBuilder command, string argument)
    {
        command.Append(' ');
        command.Append(argument);
    }

    TSelf Self => (TSelf)this;

    sealed record Argument(string Key, string Value);
}
