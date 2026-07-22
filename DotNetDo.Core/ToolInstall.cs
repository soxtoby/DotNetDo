using System.Runtime.CompilerServices;
using Serilog;

namespace DotNetDo;

/// <summary>A command-line tool that can be checked for availability and installed when missing.</summary>
public sealed record ToolInstall
{
    internal ToolInstall(string toolName, string executableName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentException.ThrowIfNullOrWhiteSpace(executableName);
        ToolName = toolName;
        ExecutableName = executableName;
    }

    /// <summary>The name used to declare the tool in <c>dotnetdo.toml</c>.</summary>
    public string ToolName { get; }

    /// <summary>The command expected on <c>PATH</c>.</summary>
    public string ExecutableName { get; }

    /// <summary>The Scoop app that provides the command, or <see langword="null"/> when DotNetDo cannot install it.</summary>
    internal string? ScoopApp { get; init; }

    /// <summary>Whether the command can currently be found on <c>PATH</c>.</summary>
    public bool IsAvailable => CommandExists(ExecutableName);

    internal static bool CommandExists(string executable) => ExecutableResolver.Find(executable) is not null;

    /// <summary>Ensures the command is available, installing it when possible.</summary>
    public TaskAwaiter GetAwaiter() => InstallAsync().GetAwaiter();

    async Task InstallAsync()
    {
        if (IsAvailable)
        {
            Log.Information("{Tool} is already available", ToolName);
            return;
        }

        if (ScoopApp is null)
            throw new ToolInstallException($"'{ToolName}' is unavailable and has no configured installer.");

        if (!OperatingSystem.IsWindows())
            throw new ToolInstallException($"'{ToolName}' is unavailable and cannot be installed: DotNetDo has no installer for this platform yet.");

        await Tools.Scoop.InstallSelf;

        Log.Information("Installing {App} with Scoop", ScoopApp);
        await (Tools.Scoop.Install with { Apps = [ScoopApp] });
        EnvironmentRefresh.RefreshPath();

        if (!IsAvailable)
            throw new ToolInstallException($"'{ExecutableName}' is still unavailable after installing Scoop app '{ScoopApp}'.");
    }
}

/// <summary>The tool was missing and could not be installed.</summary>
public sealed class ToolInstallException(string message) : Exception(message);

static class EnvironmentRefresh
{
    public static void RefreshPath()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var refreshed = EnvironmentPath.Read(EnvironmentVariableTarget.Process)
            .Concat(EnvironmentPath.Read(EnvironmentVariableTarget.Machine))
            .Concat(EnvironmentPath.Read(EnvironmentVariableTarget.User))
            .DistinctBy(Normalize, StringComparer.OrdinalIgnoreCase)
            .JoinWith(Path.PathSeparator);

        Environment.SetEnvironmentVariable("PATH", refreshed);
    }

    static string Normalize(string entry) => entry.TrimEnd(Path.DirectorySeparatorChar);
}
