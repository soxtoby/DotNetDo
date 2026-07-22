using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using Serilog;

namespace DotNetDo;

public static partial class Tools
{
    /// <summary>Scoop, a command-line package manager for Windows.</summary>
    public static class Scoop
    {
        /// <summary>Bootstraps Scoop with its official installer.</summary>
        public static ScoopInstallSelf InstallSelf => new();
        /// <summary>Installs apps from configured buckets, manifests, or manifest URLs.</summary>
        public static ScoopInstall Install => new();
        /// <summary>Uninstalls apps managed by Scoop.</summary>
        public static ScoopUninstall Uninstall => new();
        /// <summary>Updates Scoop itself or installed apps.</summary>
        public static ScoopUpdate Update => new();
        /// <summary>Manages the repositories Scoop uses to discover apps.</summary>
        public static ScoopBucket Bucket => new();
        /// <summary>Reads and changes Scoop settings.</summary>
        public static ScoopConfig Config => new();
    }
}

/// <summary>A command run through the Scoop package manager.</summary>
public abstract record ScoopCommand : ExecToolCommand
{
    internal static bool IsInstalled => ToolInstall.CommandExists("scoop");
}

/// <summary>Bootstraps Scoop with its current official installer.</summary>
public sealed record ScoopInstallSelf
{
    /// <summary>Ensures the <c>scoop</c> command is available; does nothing when it is already installed.</summary>
    public TaskAwaiter GetAwaiter() => InstallAsync().GetAwaiter();

    static async Task InstallAsync()
    {
        if (ScoopCommand.IsInstalled)
        {
            Log.Debug("Scoop is already installed");
            return;
        }

        if (!OperatingSystem.IsWindows())
            throw new ToolInstallException("Scoop is only available on Windows.");

        Log.Information("Installing Scoop with the official installer");
        await Do.Exec(InstallerCommand);
        EnvironmentRefresh.RefreshPath();

        if (!ScoopCommand.IsInstalled)
            throw new ToolInstallException("'scoop' is still unavailable after running its installer.");
    }

    // The official installer refuses elevated consoles unless -RunAsAdmin is passed explicitly.
    [SupportedOSPlatform("windows")]
    static string InstallerCommand =>
        IsElevated
            ? @"powershell -NoProfile -ExecutionPolicy Bypass -Command ""iex \""& {$(irm get.scoop.sh)} -RunAsAdmin\"""""
            : @"powershell -NoProfile -ExecutionPolicy Bypass -Command ""irm get.scoop.sh | iex""";

    [SupportedOSPlatform("windows")]
    static bool IsElevated
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}

/// <summary>Installs one or more Scoop apps.</summary>
public sealed record ScoopInstall : ScoopCommand
{
    /// <summary>App names, manifest paths, or manifest URLs to install.</summary>
    public IReadOnlyList<string> Apps { get; init => field = value.ToArray(); } = [];
    /// <summary>Installs apps globally instead of for the current user.</summary>
    public bool Global { get; init; }
    /// <summary>Skips automatic dependency installation.</summary>
    public bool Independent { get; init; }
    /// <summary>Downloads artifacts without using Scoop's download cache.</summary>
    public bool NoCache { get; init; }
    /// <summary>Prevents Scoop from updating itself before installation.</summary>
    public bool NoUpdateScoop { get; init; }
    /// <summary>Skips artifact hash validation.</summary>
    public bool SkipHashCheck { get; init; }
    /// <summary>Selects <c>32bit</c>, <c>64bit</c>, or <c>arm64</c> artifacts when supported.</summary>
    public string? Architecture { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            if (Apps.Count == 0)
                throw new InvalidOperationException("Specify at least one app to install.");
            return
                [
                    "scoop install",
                    Args(Apps),
                    Arg("--global", Global),
                    Arg("--independent", Independent),
                    Arg("--no-cache", NoCache),
                    Arg("--no-update-scoop", NoUpdateScoop),
                    Arg("--skip-hash-check", SkipHashCheck),
                    Arg("--arch", Architecture),
                ];
        }
    }
}

/// <summary>Uninstalls one or more Scoop apps.</summary>
public sealed record ScoopUninstall : ScoopCommand
{
    /// <summary>Installed app names to remove.</summary>
    public IReadOnlyList<string> Apps { get; init => field = value.ToArray(); } = [];
    /// <summary>Removes globally installed apps.</summary>
    public bool Global { get; init; }
    /// <summary>Also removes each app's persisted data.</summary>
    public bool Purge { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            if (Apps.Count == 0)
                throw new InvalidOperationException("Specify at least one app to uninstall.");
            return
                [
                    "scoop uninstall",
                    Args(Apps),
                    Arg("--global", Global),
                    Arg("--purge", Purge),
                ];
        }
    }
}

/// <summary>Updates Scoop itself or installed apps.</summary>
public sealed record ScoopUpdate : ScoopCommand
{
    /// <summary>Installed app names to update; when empty, Scoop updates itself.</summary>
    public IReadOnlyList<string> Apps { get; init => field = value.ToArray(); } = [];
    /// <summary>Updates every installed app.</summary>
    public bool All { get; init; }
    /// <summary>Updates globally installed apps.</summary>
    public bool Global { get; init; }
    /// <summary>Reinstalls apps even when no newer version exists.</summary>
    public bool Force { get; init; }
    /// <summary>Skips automatic dependency installation.</summary>
    public bool Independent { get; init; }
    /// <summary>Downloads artifacts without using Scoop's download cache.</summary>
    public bool NoCache { get; init; }
    /// <summary>Skips artifact hash validation.</summary>
    public bool SkipHashCheck { get; init; }
    /// <summary>Hides nonessential update messages.</summary>
    public bool Quiet { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            if (All && Apps.Count != 0)
                throw new InvalidOperationException("Specify either Apps or All, but not both.");
            return
                [
                    "scoop update",
                    Arg("--all", All),
                    Args(Apps),
                    Arg("--global", Global),
                    Arg("--force", Force),
                    Arg("--independent", Independent),
                    Arg("--no-cache", NoCache),
                    Arg("--skip-hash-check", SkipHashCheck),
                    Arg("--quiet", Quiet),
                ];
        }
    }
}

/// <summary>Manages Scoop buckets, the repositories from which apps are discovered.</summary>
public sealed record ScoopBucket : ScoopCommand
{
    /// <summary>The bucket operation to perform.</summary>
    public ScoopBucketCommand? Command { get; init; }
    /// <summary>An operation not represented by <see cref="ScoopBucketCommand"/>.</summary>
    public string? CustomCommand { get; init; }
    /// <summary>The bucket to add or remove.</summary>
    public string? Name { get; init; }
    /// <summary>The repository backing a custom bucket.</summary>
    public string? Repository { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            if (CustomCommand is null && Command is null)
                throw new InvalidOperationException($"Specify {nameof(Command)} or {nameof(CustomCommand)}.");
            return
                [
                    "scoop bucket",
                    Arg(CustomCommand ?? Command?.ToString().ToLowerInvariant()),
                    Arg(Name),
                    Arg(Repository),
                ];
        }
    }
}

/// <summary>An operation on Scoop's app repositories.</summary>
public enum ScoopBucketCommand
{
    /// <summary>Makes a bucket's apps available for discovery and installation.</summary>
    Add,
    /// <summary>Stops using a bucket without uninstalling its apps.</summary>
    Rm,
    /// <summary>Lists buckets currently in use.</summary>
    List,
    /// <summary>Lists official buckets that can be added without a repository URL.</summary>
    Known,
}

/// <summary>Reads, writes, or removes settings in Scoop's configuration file.</summary>
public sealed record ScoopConfig : ScoopCommand
{
    /// <summary>The setting to read, write, or remove; when omitted, all settings are shown.</summary>
    public string? Name { get; init; }
    /// <summary>The new setting value; when omitted, the current value is shown.</summary>
    public string? Value { get; init; }
    /// <summary>Removes the named setting.</summary>
    public bool Remove { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            if (Remove && string.IsNullOrWhiteSpace(Name))
                throw new InvalidOperationException($"{nameof(Remove)} requires {nameof(Name)}.");
            if (Value is not null && string.IsNullOrWhiteSpace(Name))
                throw new InvalidOperationException($"{nameof(Value)} requires {nameof(Name)}.");
            if (Remove && Value is not null)
                throw new InvalidOperationException($"Specify either {nameof(Value)} or {nameof(Remove)}, but not both.");
            return
                [
                    "scoop config",
                    Arg("rm", Remove),
                    Arg(Name),
                    Arg(Value),
                ];
        }
    }
}
