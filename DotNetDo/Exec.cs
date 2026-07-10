using System.Diagnostics;
using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Events;

namespace DotNetDo;

public static partial class Do
{
    public static ExecProcess Exec(ToolCommand command, ExecOptions? options = null) => Exec(command.ToString(), options);

    public static ExecProcess Exec(string command, ExecOptions? options = null)
    {
        options ??= new ExecOptions();
        var workingDirectory = options.WorkingDirectory ?? Environment.CurrentDirectory;
        var parsed = ExecCommand.Parse(command);
        var startInfo = new ProcessStartInfo(parsed.Program, parsed.Arguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory,
            };

        Log.Debug("Executing {Command} in {WorkingDirectory}", command, workingDirectory);

        try
        {
            var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start command '{command}'.");

            return new ExecProcess(process, command, workingDirectory, options.Log ?? ExecOptions.DefaultLog);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to start command {Command}", command);
            throw;
        }
    }
}

public sealed record ExecOptions
{
    public string? WorkingDirectory { get; init; }
    public Action<OutputType, string>? Log { get; init; }

    internal static void DefaultLog(OutputType type, string message) =>
        Serilog.Log.Write(
            type == OutputType.Out ? LogEventLevel.Information : LogEventLevel.Error,
            "{ToolOutput:l}",
            message);
}

sealed record ExecCommand(string Program, string Arguments)
{
    public static ExecCommand Parse(string command)
    {
        var trimmed = command.TrimStart();
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
    internal ExecProcess(
        Process process,
        string command,
        string workingDirectory,
        Action<OutputType, string> log)
    {
        var output = new ExecCapture(log);
        Output = output.Stream;
        Completed = CompleteAsync(process, command, workingDirectory, output);
        Succeeded = EnsureSuccessAsync(Completed);
    }

    public IAsyncEnumerable<ExecOutput> Output { get; }
    public Task<ExecResult> Completed { get; }
    public Task<ExecResult> Succeeded { get; }

    public TaskAwaiter<ExecResult> GetAwaiter() => Succeeded.GetAwaiter();

    static async Task<ExecResult> CompleteAsync(
        Process process,
        string command,
        string workingDirectory,
        ExecCapture output)
    {
        using (process)
        {
            try
            {
                await Task.WhenAll(
                    process.WaitForExitAsync(),
                    output.ReadAsync(process.StandardOutput, OutputType.Out),
                    output.ReadAsync(process.StandardError, OutputType.Error));

                var result = new ExecResult
                    {
                        Command = command,
                        WorkingDirectory = workingDirectory,
                        Output = output.Snapshot,
                        ExitCode = process.ExitCode,
                    };

                output.Complete();

                if (result.ExitCode == 0)
                    Log.Debug("Command {Command} completed successfully", command);
                else
                    Log.Error("Command {Command} failed with exit code {ExitCode}", command, result.ExitCode);

                return result;
            }
            catch (Exception exception)
            {
                output.Complete(exception);
                throw;
            }
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
    public required ExecOutput[] Output { get; init; }
    public required int ExitCode { get; init; }
}

public sealed class ExecFailedException(ExecResult result) : Exception($"Command '{result.Command}' failed with exit code {result.ExitCode}.")
{
    public ExecResult Result { get; } = result;
    public string Command { get; } = result.Command;
}

sealed class ExecCapture(Action<OutputType, string> log)
{
    readonly ReplayStream<ExecOutput> _stream = new();
    readonly List<ExecOutput> _snapshot = [];
    readonly Lock _gate = new();

    public IAsyncEnumerable<ExecOutput> Stream => _stream;

    public ExecOutput[] Snapshot
    {
        get
        {
            lock (_gate)
                return [.._snapshot];
        }
    }

    public async Task ReadAsync(StreamReader reader, OutputType type)
    {
        while (await reader.ReadLineAsync() is { } message)
            Append(type, message);
    }

    void Append(OutputType type, string message)
    {
        var output = new ExecOutput(type, message);

        lock (_gate)
        {
            _snapshot.Add(output);
            _stream.Append(output);
        }

        log(type, message);
    }

    public void Complete(Exception? exception = null) => _stream.Complete(exception);
}

public sealed record ExecOutput(OutputType Type, string Message);

public enum OutputType
{
    Out,
    Error,
}