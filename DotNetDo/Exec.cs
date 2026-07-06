using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace DotNetDo;

public static partial class Do
{
    public static ExecProcess Exec(string command, ExecOptions? options = null)
    {
        var workingDirectory = options?.WorkingDirectory ?? Environment.CurrentDirectory;
        var parsed = ExecCommand.Parse(command);
        var startInfo = new ProcessStartInfo(parsed.Program, parsed.Arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory,
            };

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start command '{command}'.");

        return new ExecProcess(process, command, workingDirectory);
    }
}

public sealed record ExecOptions
{
    public string? WorkingDirectory { get; init; }
}

sealed record ExecCommand(string Program, string Arguments)
{
    public static ExecCommand Parse(string command)
    {
        var trimmed = command.Trim();
        return trimmed.StartsWith('"')
            ? ParseQuoted(trimmed)
            : ParseUnquoted(trimmed);
    }

    static ExecCommand ParseQuoted(string command)
    {
        var closingQuote = command.IndexOf('"', 1);
        return closingQuote > 0
            ? new ExecCommand(command[1..closingQuote], command[(closingQuote + 1)..].TrimStart())
            : throw new ArgumentException("Command has an opening quote without a closing quote.", nameof(command));
    }

    static ExecCommand ParseUnquoted(string command)
    {
        var firstWhitespace = command.IndexOfAny([' ', '\t', '\r', '\n']);
        return firstWhitespace >= 0
            ? new ExecCommand(command[..firstWhitespace], command[firstWhitespace..].TrimStart())
            : new ExecCommand(command, "");
    }
}

public sealed class ExecProcess
{
    internal ExecProcess(Process process, string command, string workingDirectory)
    {
        var output = new ExecOutput();
        var error = new ExecOutput();

        Output = output.Lines;
        Error = error.Lines;
        OutputChunks = output.Chunks;
        ErrorChunks = error.Chunks;

        Completed = CompleteAsync(process, command, workingDirectory, output, error);
        Succeeded = EnsureSuccessAsync(Completed);
    }

    public IAsyncEnumerable<string> Output { get; }
    public IAsyncEnumerable<string> Error { get; }
    public IAsyncEnumerable<string> OutputChunks { get; }
    public IAsyncEnumerable<string> ErrorChunks { get; }
    public Task<ExecResult> Completed { get; }
    public Task<ExecResult> Succeeded { get; }

    public TaskAwaiter<ExecResult> GetAwaiter() => Succeeded.GetAwaiter();

    static async Task<ExecResult> CompleteAsync(
        Process process,
        string command,
        string workingDirectory,
        ExecOutput output,
        ExecOutput error)
    {
        using (process)
        {
            var outputTask = output.ReadAsync(process.StandardOutput);
            var errorTask = error.ReadAsync(process.StandardError);

            await Task.WhenAll(process.WaitForExitAsync(), outputTask, errorTask);

            return new ExecResult
                {
                    Command = command,
                    WorkingDirectory = workingDirectory,
                    Output = output.LinesSnapshot,
                    Error = error.LinesSnapshot,
                    OutputText = output.Text,
                    ErrorText = error.Text,
                    ExitCode = process.ExitCode,
                };
        }
    }

    static async Task<ExecResult> EnsureSuccessAsync(Task<ExecResult> completed)
    {
        var result = await completed;
        return result.ExitCode == 0
            ? result
            : throw new ExecFailedException(result);
    }
}

public sealed record ExecResult
{
    public required string Command { get; init; }
    public required string WorkingDirectory { get; init; }
    public required string[] Output { get; init; }
    public required string[] Error { get; init; }
    public required string OutputText { get; init; }
    public required string ErrorText { get; init; }
    public required int ExitCode { get; init; }
}

public sealed class ExecFailedException(ExecResult result) : Exception($"Command '{result.Command}' failed with exit code {result.ExitCode}.")
{
    public ExecResult Result { get; } = result;
    public string Command { get; } = result.Command;
}

sealed class ExecOutput
{
    readonly ReplayTextStream _lines = new();
    readonly ReplayTextStream _chunks = new();
    readonly StringBuilder _text = new();
    readonly List<string> _lineSnapshot = [];
    readonly Lock _gate = new();

    public IAsyncEnumerable<string> Lines => _lines;
    public IAsyncEnumerable<string> Chunks => _chunks;

    public string[] LinesSnapshot
    {
        get
        {
            lock (_gate)
                return [.._lineSnapshot];
        }
    }

    public string Text
    {
        get
        {
            lock (_gate)
                return _text.ToString();
        }
    }

    public async Task ReadAsync(StreamReader reader)
    {
        var buffer = new char[4096];
        var line = new StringBuilder();
        var pendingCarriageReturn = false;
        var count = 1;

        try
        {
            while (count > 0)
            {
                count = await reader.ReadAsync(buffer);
                if (count > 0)
                {
                    var chunk = new string(buffer, 0, count);
                    AppendChunk(chunk);
                    _chunks.Append(chunk);

                    foreach (var character in chunk)
                    {
                        if (pendingCarriageReturn && character != '\n')
                        {
                            AppendLine(line.ToString());
                            line.Clear();
                        }

                        pendingCarriageReturn = (!pendingCarriageReturn || character != '\n') && pendingCarriageReturn;

                        if (character == '\r')
                        {
                            pendingCarriageReturn = true;
                        }
                        else if (character == '\n' && !pendingCarriageReturn)
                        {
                            AppendLine(line.ToString());
                            line.Clear();
                        }
                        else if (character != '\n')
                        {
                            line.Append(character);
                            pendingCarriageReturn = false;
                        }
                    }
                }
            }

            if (pendingCarriageReturn || line.Length > 0)
                AppendLine(line.ToString());

            _lines.Complete();
            _chunks.Complete();
        }
        catch (Exception exception)
        {
            _lines.Complete(exception);
            _chunks.Complete(exception);
            throw;
        }
    }

    void AppendChunk(string chunk)
    {
        lock (_gate)
            _text.Append(chunk);
    }

    void AppendLine(string line)
    {
        lock (_gate)
            _lineSnapshot.Add(line);

        _lines.Append(line);
    }
}

sealed class ReplayTextStream : IAsyncEnumerable<string>
{
    readonly Lock _gate = new();
    readonly List<string> _items = [];
    readonly List<Channel<string>> _subscribers = [];
    Exception? _exception;
    bool _completed;

    public async IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<string>();

        lock (_gate)
        {
            foreach (var item in _items)
                channel.Writer.TryWrite(item);

            if (_completed)
            {
                channel.Writer.TryComplete(_exception);
            }
            else
            {
                _subscribers.Add(channel);
            }
        }

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync().WithCancellation(cancellationToken))
                yield return item;
        }
        finally
        {
            lock (_gate)
                _subscribers.Remove(channel);
        }
    }

    public void Append(string item)
    {
        Channel<string>[] subscribers;
        lock (_gate)
        {
            if (!_completed)
            {
                _items.Add(item);
                subscribers = [.._subscribers];
            }
            else
            {
                subscribers = [];
            }
        }

        foreach (var subscriber in subscribers)
            subscriber.Writer.TryWrite(item);
    }

    public void Complete(Exception? exception = null)
    {
        Channel<string>[] subscribers;
        lock (_gate)
        {
            if (!_completed)
            {
                _completed = true;
                _exception = exception;
                subscribers = [.. _subscribers];
            }
            else
            {
                subscribers = [];
            }
        }

        foreach (var subscriber in subscribers)
            subscriber.Writer.TryComplete(exception);
    }
}
