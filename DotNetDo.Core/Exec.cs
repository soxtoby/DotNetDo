using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Serilog;

namespace DotNetDo;

/// <summary>Provides the task-authoring entry points and workspace-scoped defaults.</summary>
public static partial class Do
{
    /// <summary>Starts the rendered command in the configured working directory, using the native batch host when required.</summary>
    /// <param name="command">A typed command whose canonical rendering and execution options are used.</param>
    public static ExecProcess Exec(ToolCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Exec(command.ToString(), command);
    }

    /// <summary>Starts the command in the configured working directory, using the native batch host when required.</summary>
    /// <param name="command">One executable and its arguments. The first token may be quoted; shell operators and expansion are not interpreted.</param>
    /// <param name="options">Optional working-directory, child-environment, and per-line logging behavior.</param>
    public static ExecProcess Exec(string command, ExecOptions? options = null)
    {
        options ??= new ExecOptions();
        var workingDirectory = options.WorkingDirectory ?? Do.WorkingDirectory;
        var parsed = ExecCommand.Parse(command);
        var resolvedProgram = ExecutableResolver.Find(parsed.Program, workingDirectory);
        var startInfo = CreateStartInfo(resolvedProgram, parsed.Program, parsed.Arguments);
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.WorkingDirectory = workingDirectory;

        if (options.Environment is { } transformEnvironment)
            ApplyEnvironment(startInfo, transformEnvironment);

        if (options.LogCommand)
            Log.Debug("Executing {Command} in {WorkingDirectory}", command, workingDirectory);

        try
        {
            var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException($"Failed to start command '{command}'.");

            return new ExecProcess(process, command, workingDirectory, options.Log ?? ExecLog.Default);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to start command {Command}", command);
            throw;
        }
    }

    static ProcessStartInfo CreateStartInfo(AbsolutePath? resolvedProgram, string program, string arguments)
    {
        if (!OperatingSystem.IsWindows() || resolvedProgram is null || !ExecutableResolver.IsBatchFile(resolvedProgram))
            return new(resolvedProgram?.ToString() ?? program, arguments);

        var command = $"\"{resolvedProgram}\"";
        if (arguments.Length != 0)
            command += " " + arguments;
        return new(Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe", $"/d /s /c \"{command}\"");
    }

    static void ApplyEnvironment(
        ProcessStartInfo startInfo,
        Func<ImmutableDictionary<string, string>, ImmutableDictionary<string, string>> transform)
    {
        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var current = startInfo.Environment.ToImmutableDictionary(
            pair => pair.Key,
            pair => pair.Value ?? throw InvalidProcessEnvironment("The inherited process environment contains a null value."),
            comparer);
        var transformed = transform(current)
            ?? throw InvalidProcessEnvironment("The process environment transformation returned null.");
        var environment = new Dictionary<string, string>(comparer);

        foreach (var pair in transformed)
        {
            if (pair.Value is null)
                throw InvalidProcessEnvironment("The process environment transformation returned a null value.");
            if (!environment.TryAdd(pair.Key, pair.Value))
                throw InvalidProcessEnvironment("The process environment transformation returned duplicate variable names under the current operating system's comparison rules.");
        }

        startInfo.Environment.Clear();
        foreach (var pair in environment)
            startInfo.Environment.Add(pair.Key, pair.Value);
    }

    static InvalidOperationException InvalidProcessEnvironment(string message) => new(message);
}

/// <summary>Controls process working directory, environment, and output logging.</summary>
public record ExecOptions
{
    /// <summary>The directory in which the operation executes.</summary>
    public AbsolutePath? WorkingDirectory { get; init; }
    /// <summary>Transforms an immutable snapshot of the current process environment into the complete child environment for each launch attempt.</summary>
    public Func<ImmutableDictionary<string, string>, ImmutableDictionary<string, string>>? Environment { get; init; }
    /// <summary>Whether to log the command and working directory before execution.</summary>
    public bool LogCommand { get; init; } = true;
    /// <summary>Controls standard-output and standard-error logging; when omitted, <see cref="ExecLog.Default"/> is used.</summary>
    public ExecLog? Log { get; init; }
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

/// <summary>A running process with replayable output and separate completion and success tasks.</summary>
public sealed class ExecProcess
{
    internal ExecProcess(
        Process process,
        string command,
        string workingDirectory,
        ExecLog log)
    {
        var output = new ExecCapture(log);
        Output = output.Stream;
        Completed = CompleteAsync(process, command, workingDirectory, output);
        Succeeded = EnsureSuccessAsync(Completed);
    }

    /// <summary>The process output stream in arrival order.</summary>
    public IAsyncEnumerable<ExecOutput> Output { get; }
    /// <summary>Completes with the result for every exit code.</summary>
    public Task<ExecResult> Completed { get; }
    /// <summary>Completes with the result only for exit code zero; otherwise throws <see cref="ExecFailedException"/>.</summary>
    public Task<ExecResult> Succeeded { get; }

    /// <summary>Allows awaiting the command and throws when the process exits unsuccessfully.</summary>
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
                        AllOutput = output.Snapshot,
                        ExitCode = process.ExitCode,
                    };

                output.Complete();

                if (result.ExitCode != 0)
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

/// <summary>The captured command, directory, output, and exit code of a finished process.</summary>
public sealed partial record ExecResult
{
    /// <summary>The exact rendered command text.</summary>
    public required string Command { get; init; }
    /// <summary>The directory in which the operation executes.</summary>
    public required string WorkingDirectory { get; init; }
    /// <summary>The process output stream in arrival order.</summary>
    public required ExecOutput[] AllOutput { get; init; }
    /// <summary>The operating-system process exit code.</summary>
    public required int ExitCode { get; init; }
}

/// <summary>Thrown when an awaited command exits with a non-zero code.</summary>
public sealed class ExecFailedException(ExecResult result) : Exception($"Command '{result.Command}' failed with exit code {result.ExitCode}.")
{
    /// <summary>The failed process result.</summary>
    public ExecResult Result { get; } = result;
    /// <summary>The exact rendered command text.</summary>
    public string Command { get; } = result.Command;
}

sealed class ExecCapture(ExecLog log)
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

        log.Write(type, message);
    }

    public void Complete(Exception? exception = null) => _stream.Complete(exception);
}

/// <summary>One line read from a process output stream.</summary>
public sealed record ExecOutput(OutputType Type, string Message);

/// <summary>Identifies the process stream from which a line was read.</summary>
public enum OutputType
{
    /// <summary>Standard output.</summary>
    Out,
    /// <summary>Standard error.</summary>
    Error,
}
