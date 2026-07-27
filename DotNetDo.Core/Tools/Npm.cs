namespace DotNetDo;

public static partial class Tools
{
    /// <summary>Provides fresh commands for npm's core package workflow. npm itself must be supplied on <c>PATH</c>.</summary>
    public static class Npm
    {
        /// <summary>Installs dependencies or adds packages to a project.</summary>
        public static NpmInstall Install => new();
        /// <summary>Performs a clean, lockfile-based dependency install.</summary>
        public static NpmCleanInstall CleanInstall => new();
        /// <summary>Runs a package script.</summary>
        public static NpmRun Run => new();
        /// <summary>Runs the package test script.</summary>
        public static NpmTest Test => new();
        /// <summary>Creates a package tarball.</summary>
        public static NpmPack Pack => new();
        /// <summary>Publishes a package to the configured registry.</summary>
        public static NpmPublish Publish => new();
    }
}

/// <summary>Common npm workspace selection options.</summary>
public abstract record NpmWorkspaceCommand : ExecToolCommand
{
    /// <summary>Workspace names in which npm runs the command.</summary>
    public IReadOnlyList<string> Workspaces { get; init => field = [..value]; } = [];
    /// <summary>Runs the command in every configured workspace.</summary>
    public bool AllWorkspaces { get; init; }
    /// <summary>Includes the workspace root when workspace selection is active.</summary>
    public bool IncludeWorkspaceRoot { get; init; }

    /// <summary>Renders workspace options in npm's canonical order.</summary>
    protected IReadOnlyList<string?> WorkspaceParts =>
        [
            Args("--workspace", Workspaces),
            Arg("--workspaces", AllWorkspaces),
            Arg("--include-workspace-root", IncludeWorkspaceRoot),
        ];

    /// <summary>Rejects npm's mutually exclusive named and all-workspace selectors.</summary>
    protected void ValidateWorkspaces()
    {
        if (AllWorkspaces && Workspaces.Count != 0)
            throw new InvalidOperationException($"Specify either {nameof(Workspaces)} or {nameof(AllWorkspaces)}, but not both.");
    }
}

/// <summary>Shared options for npm install operations.</summary>
public abstract record NpmInstallCommand : NpmWorkspaceCommand
{
    /// <summary>Controls npm's dependency layout.</summary>
    public NpmInstallStrategy? InstallStrategy { get; init; }
    /// <summary>Dependency types omitted from disk.</summary>
    public IReadOnlyList<NpmDependencyType> Omit { get; init => field = [..value]; } = [];
    /// <summary>Dependency types included on disk.</summary>
    public IReadOnlyList<NpmDependencyType> Include { get; init => field = [..value]; } = [];
    /// <summary>Fails on conflicting peer dependencies.</summary>
    public bool StrictPeerDependencies { get; init; }
    /// <summary>Runs lifecycle scripts in the foreground.</summary>
    public bool ForegroundScripts { get; init; }
    /// <summary>Skips package lifecycle scripts.</summary>
    public bool IgnoreScripts { get; init; }
    /// <summary>Controls audit submission; null leaves npm configuration unchanged.</summary>
    public bool? Audit { get; init; }
    /// <summary>Controls funding notices; null leaves npm configuration unchanged.</summary>
    public bool? Fund { get; init; }
    /// <summary>Reports intended changes without applying them.</summary>
    public bool DryRun { get; init; }

    /// <summary>Renders shared install options.</summary>
    protected IReadOnlyList<string?> InstallParts =>
        [
            Arg("--install-strategy", InstallStrategy),
            Args("--omit", Omit),
            Args("--include", Include),
            Arg("--strict-peer-deps", StrictPeerDependencies),
            Arg("--foreground-scripts", ForegroundScripts),
            Arg("--ignore-scripts", IgnoreScripts),
            Arg("--audit", "--no-audit", Audit),
            Arg("--fund", "--no-fund", Fund),
            Arg("--dry-run", DryRun),
        ];
}

/// <summary>npm dependency types accepted by install filtering.</summary>
public enum NpmDependencyType
{
    /// <summary>Production dependencies.</summary>
    Prod,
    /// <summary>Development dependencies.</summary>
    Dev,
    /// <summary>Optional dependencies.</summary>
    Optional,
    /// <summary>Peer dependencies.</summary>
    Peer,
}

/// <summary>How npm arranges installed dependencies.</summary>
public enum NpmInstallStrategy
{
    /// <summary>Hoists non-duplicated dependencies to the top level.</summary>
    Hoisted,
    /// <summary>Installs dependencies in place without hoisting.</summary>
    Nested,
    /// <summary>Installs only direct dependencies at the top level.</summary>
    Shallow,
    /// <summary>Links dependencies into the project from a shared store.</summary>
    Linked,
}

/// <summary>Installs project dependencies or adds package specifications.</summary>
public sealed record NpmInstall : NpmInstallCommand
{
    /// <summary>Package specifications to add; empty installs the current project.</summary>
    public IReadOnlyList<string> Packages { get; init => field = [..value]; } = [];
    /// <summary>Saves added packages as development dependencies.</summary>
    public bool SaveDev { get; init; }
    /// <summary>Saves exact versions rather than ranges.</summary>
    public bool SaveExact { get; init; }
    /// <summary>Uses npm's global install prefix.</summary>
    public bool Global { get; init; }
    /// <summary>Writes only the lockfile without installing dependencies.</summary>
    public bool PackageLockOnly { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            ValidateWorkspaces();
            return
                [
                    "npm install",
                    Args(Packages),
                    Arg("--save-dev", SaveDev),
                    Arg("--save-exact", SaveExact),
                    Arg("--global", Global),
                    ..InstallParts,
                    Arg("--package-lock-only", PackageLockOnly),
                    ..WorkspaceParts,
                ];
        }
    }
}

/// <summary>Performs npm's clean install using the existing lockfile.</summary>
public sealed record NpmCleanInstall : NpmInstallCommand
{
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            ValidateWorkspaces();
            return ["npm ci", ..InstallParts, ..WorkspaceParts];
        }
    }
}

/// <summary>Runs a named package script and optionally forwards arguments after <c>--</c>.</summary>
public sealed record NpmRun : NpmWorkspaceCommand
{
    /// <summary>The package script name.</summary>
    public string? Script { get; init; }
    /// <summary>Arguments forwarded verbatim as individual semantic values to the script.</summary>
    public IReadOnlyList<string> Arguments { get; init => field = [..value]; } = [];
    /// <summary>Succeeds when the named script is absent.</summary>
    public bool IfPresent { get; init; }
    /// <summary>The shell used to execute scripts.</summary>
    public string? ScriptShell { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Script);
            ValidateWorkspaces();
            return
                [
                    "npm run",
                    Arg(Script),
                    ..WorkspaceParts,
                    Arg("--if-present", IfPresent),
                    Arg("--script-shell", ScriptShell),
                    Arguments.Count == 0 ? null : "--",
                    Args(Arguments),
                ];
        }
    }
}

/// <summary>Runs the package's test script.</summary>
public sealed record NpmTest : ExecToolCommand
{
    /// <summary>Arguments forwarded to the test script after <c>--</c>.</summary>
    public IReadOnlyList<string> Arguments { get; init => field = [..value]; } = [];
    /// <summary>Skips package lifecycle scripts.</summary>
    public bool IgnoreScripts { get; init; }
    /// <summary>The shell used to execute scripts.</summary>
    public string? ScriptShell { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "npm test",
            Arg("--ignore-scripts", IgnoreScripts),
            Arg("--script-shell", ScriptShell),
            Arguments.Count == 0 ? null : "--",
            Args(Arguments),
        ];
}

/// <summary>Creates an npm package tarball.</summary>
public sealed record NpmPack : NpmWorkspaceCommand
{
    /// <summary>Package specification to pack; null packs the current project.</summary>
    public string? Package { get; init; }
    /// <summary>Reports pack contents without writing a tarball.</summary>
    public bool DryRun { get; init; }
    /// <summary>Requests npm's JSON output.</summary>
    public bool Json { get; init; }
    /// <summary>Directory in which npm writes the tarball.</summary>
    public string? Destination { get; init; }
    /// <summary>Skips package lifecycle scripts.</summary>
    public bool IgnoreScripts { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            ValidateWorkspaces();
            return
                [
                    "npm pack",
                    Arg(Package),
                    Arg("--dry-run", DryRun),
                    Arg("--json", Json),
                    Arg("--pack-destination", Destination),
                    ..WorkspaceParts,
                    Arg("--ignore-scripts", IgnoreScripts),
                ];
        }
    }
}

/// <summary>Publishes an npm package to the configured registry.</summary>
public sealed record NpmPublish : NpmWorkspaceCommand
{
    /// <summary>Package specification to publish; null publishes the current project.</summary>
    public string? Package { get; init; }
    /// <summary>Distribution tag applied to the published version.</summary>
    public string? Tag { get; init; }
    /// <summary>Registry access level for the package.</summary>
    public NpmAccess? Access { get; init; }
    /// <summary>Reports intended publication without uploading.</summary>
    public bool DryRun { get; init; }
    /// <summary>One-time password used for registry authentication.</summary>
    public string? Otp { get; init; }
    /// <summary>Generates provenance when supported by the CI environment.</summary>
    public bool Provenance { get; init; }
    /// <summary>Uses a pre-generated provenance statement instead.</summary>
    public string? ProvenanceFile { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            ValidateWorkspaces();
            if (Provenance && ProvenanceFile is not null)
                throw new InvalidOperationException($"Specify either {nameof(Provenance)} or {nameof(ProvenanceFile)}, but not both.");
            return
                [
                    "npm publish",
                    Arg(Package),
                    Arg("--tag", Tag),
                    Arg("--access", Access),
                    Arg("--dry-run", DryRun),
                    Arg("--otp", Otp),
                    ..WorkspaceParts,
                    Arg("--provenance", Provenance),
                    Arg("--provenance-file", ProvenanceFile),
                ];
        }
    }
}

/// <summary>npm registry visibility for a published package.</summary>
public enum NpmAccess
{
    /// <summary>Visible only to authorized users.</summary>
    Restricted,
    /// <summary>Publicly visible.</summary>
    Public,
    /// <summary>Private registry visibility where supported.</summary>
    Private,
}