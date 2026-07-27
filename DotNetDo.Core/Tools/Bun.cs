using Serilog.Events;

namespace DotNetDo;

public static partial class Tools
{
    /// <summary>Provides fresh commands for Bun's core package, script, test, and build workflows. Bun must be supplied on <c>PATH</c>.</summary>
    public static class Bun
    {
        internal const string ToolName = "bun";

        /// <summary>Makes the <c>bun</c> command available.</summary>
        public static ToolInstall EnsureAvailable => new(ToolName, "bun") { ScoopApp = "bun" };
        /// <summary>Installs project dependencies.</summary>
        public static BunInstall Install => new();
        /// <summary>Adds packages to a project.</summary>
        public static BunAdd Add => new();
        /// <summary>Runs a file or package script.</summary>
        public static BunRun Run => new();
        /// <summary>Runs Bun tests.</summary>
        public static BunTest Test => new();
        /// <summary>Bundles application entry points.</summary>
        public static BunBuild Build => new();
        /// <summary>Publishes a package to an npm registry.</summary>
        public static BunPublish Publish => new();
    }
}

/// <summary>Common Bun output-volume controls.</summary>
public abstract record BunCommand : ExecToolCommand
{
    /// <summary>Limits Bun's output.</summary>
    public bool Quiet { get; init; }
    /// <summary>Enables Bun's most detailed output.</summary>
    public bool Verbose { get; init; }

    /// <summary>Snapshots DotNetDo's logging preference into Bun's native controls.</summary>
    protected BunCommand() => (Quiet, Verbose) = Logging.Level switch
    {
        LogEventLevel.Verbose or LogEventLevel.Debug => (false, true),
        LogEventLevel.Warning or LogEventLevel.Error or LogEventLevel.Fatal => (true, false),
        _ => (false, false),
    };

    /// <summary>Renders Bun's output-volume controls.</summary>
    protected IReadOnlyList<string?> OutputParts => [Arg("--quiet", Quiet), Arg("--verbose", Verbose)];
}

/// <summary>Shared options for Bun dependency installation operations.</summary>
public abstract record BunDependencyCommand : BunCommand
{
    /// <summary>Package specifications operated on by the command.</summary>
    public IReadOnlyList<string> Packages { get; init => field = [.. value]; } = [];
    /// <summary>Reports intended changes without applying them.</summary>
    public bool DryRun { get; init; }
    /// <summary>Disallows lockfile changes.</summary>
    public bool FrozenLockfile { get; init; }
    /// <summary>Forces dependency resolution and reinstallation.</summary>
    public bool Force { get; init; }
    /// <summary>Skips lifecycle scripts.</summary>
    public bool IgnoreScripts { get; init; }
    /// <summary>Installs globally.</summary>
    public bool Global { get; init; }
    /// <summary>Registry used instead of configured defaults.</summary>
    public string? Registry { get; init; }
    /// <summary>Dependency types omitted from installation.</summary>
    public IReadOnlyList<BunDependencyType> Omit { get; init => field = [.. value]; } = [];
    /// <summary>Dependency linker strategy.</summary>
    public BunLinker? Linker { get; init; }

    /// <summary>Renders shared dependency options in Bun help order.</summary>
    protected IReadOnlyList<string?> DependencyParts =>
        [
            Arg("--dry-run", DryRun),
            Arg("--frozen-lockfile", FrozenLockfile),
            Arg("--force", Force),
            ..OutputParts,
            Arg("--ignore-scripts", IgnoreScripts),
            Arg("--global", Global),
            Arg("--registry", Registry),
            ..Omit.Select(value => Arg("--omit", value)),
            Arg("--linker", Linker),
        ];
}

/// <summary>Dependency category accepted by Bun's omit option.</summary>
public enum BunDependencyType
{
    /// <summary>Development dependencies.</summary>
    Dev,
    /// <summary>Optional dependencies.</summary>
    Optional,
    /// <summary>Peer dependencies.</summary>
    Peer,
}

/// <summary>Bun dependency linker strategy.</summary>
public enum BunLinker
{
    /// <summary>Uses a central store with isolated dependency trees.</summary>
    Isolated,
    /// <summary>Uses a conventional hoisted dependency tree.</summary>
    Hoisted,
}

/// <summary>Installs dependencies from the current package manifest or supplied package specifications.</summary>
public sealed record BunInstall : BunDependencyCommand
{
    /// <summary>Excludes development dependencies.</summary>
    public bool Production { get; init; }
    /// <summary>Writes only the lockfile.</summary>
    public bool LockfileOnly { get; init; }
    /// <summary>Workspace filters; repeat the native option once per value.</summary>
    public IReadOnlyList<string> Filters { get; init => field = [.. value]; } = [];

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "bun install",
            Args(Packages),
            Arg("--production", Production),
            ..DependencyParts,
            Arg("--lockfile-only", LockfileOnly),
            ..Filters.Select(value => Arg("--filter", value)),
        ];
}

/// <summary>Adds packages to the current package manifest and installs them.</summary>
public sealed record BunAdd : BunDependencyCommand
{
    /// <summary>Saves packages as development dependencies.</summary>
    public bool Dev { get; init; }
    /// <summary>Saves packages as optional dependencies.</summary>
    public bool Optional { get; init; }
    /// <summary>Saves packages as peer dependencies.</summary>
    public bool Peer { get; init; }
    /// <summary>Saves exact versions.</summary>
    public bool Exact { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            if (Packages.Count == 0)
                throw new InvalidOperationException($"{nameof(Packages)} must contain at least one package.");
            if ((Dev ? 1 : 0) + (Optional ? 1 : 0) + (Peer ? 1 : 0) > 1)
                throw new InvalidOperationException($"Specify only one of {nameof(Dev)}, {nameof(Optional)}, or {nameof(Peer)}.");
            return
                [
                    "bun add",
                    Args(Packages),
                    ..DependencyParts,
                    Arg("--dev", Dev),
                    Arg("--optional", Optional),
                    Arg("--peer", Peer),
                    Arg("--exact", Exact),
                ];
        }
    }
}

/// <summary>Runs a Bun file or package script and forwards trailing arguments.</summary>
public sealed record BunRun : ExecToolCommand
{
    /// <summary>File or package script to run.</summary>
    public string? Target { get; init; }
    /// <summary>Arguments forwarded to the target.</summary>
    public IReadOnlyList<string> Arguments { get; init => field = [.. value]; } = [];
    /// <summary>Does not print the script command.</summary>
    public bool Silent { get; init; }
    /// <summary>Runs the script in every workspace matching each filter.</summary>
    public IReadOnlyList<string> Filters { get; init => field = [.. value]; } = [];
    /// <summary>Runs the script in every configured workspace.</summary>
    public bool Workspaces { get; init; }
    /// <summary>Forces scripts and packages to use the Bun runtime.</summary>
    public bool UseBun { get; init; }
    /// <summary>Exits successfully when the target is absent.</summary>
    public bool IfPresent { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Target);
            return
                [
                    "bun run",
                    Arg(Target),
                    Arg("--silent", Silent),
                    ..Filters.Select(value => Arg("--filter", value)),
                    Arg("--workspaces", Workspaces),
                    Arg("--bun", UseBun),
                    Arg("--if-present", IfPresent),
                    Args(Arguments),
                ];
        }
    }
}

/// <summary>Runs Bun's test runner.</summary>
public sealed record BunTest : ExecToolCommand
{
    /// <summary>File-name patterns selecting tests.</summary>
    public IReadOnlyList<string> Patterns { get; init => field = [.. value]; } = [];
    /// <summary>Per-test timeout in milliseconds.</summary>
    public int? Timeout { get; init; }
    /// <summary>Updates snapshots.</summary>
    public bool UpdateSnapshots { get; init; }
    /// <summary>Includes todo tests.</summary>
    public bool Todo { get; init; }
    /// <summary>Generates a coverage profile.</summary>
    public bool Coverage { get; init; }
    /// <summary>Exits after this many failures.</summary>
    public int? Bail { get; init; }
    /// <summary>Runs only test names matching this expression.</summary>
    public string? TestNamePattern { get; init; }
    /// <summary>Exits successfully when no tests are found.</summary>
    public bool PassWithNoTests { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "bun test",
            Args(Patterns),
            Arg("--timeout", Timeout),
            Arg("--update-snapshots", UpdateSnapshots),
            Arg("--todo", Todo),
            Arg("--coverage", Coverage),
            Arg("--bail", Bail),
            Arg("--test-name-pattern", TestNamePattern),
            Arg("--pass-with-no-tests", PassWithNoTests),
        ];
}

/// <summary>Bundles one or more application entry points.</summary>
public sealed record BunBuild : ExecToolCommand
{
    /// <summary>Entry points to bundle.</summary>
    public IReadOnlyList<string> EntryPoints { get; init => field = [.. value]; } = [];
    /// <summary>Enables production defaults.</summary>
    public bool Production { get; init; }
    /// <summary>Generates a standalone executable.</summary>
    public bool Compile { get; init; }
    /// <summary>Intended execution environment.</summary>
    public BunBuildTarget? Target { get; init; }
    /// <summary>Directory for multiple build outputs.</summary>
    public string? OutDir { get; init; }
    /// <summary>File for a single build output.</summary>
    public string? OutFile { get; init; }
    /// <summary>Enables code splitting.</summary>
    public bool Splitting { get; init; }
    /// <summary>Enables all minification passes.</summary>
    public bool Minify { get; init; }
    /// <summary>Module output format.</summary>
    public BunBuildFormat? Format { get; init; }
    /// <summary>Package specifiers excluded from bundling.</summary>
    public IReadOnlyList<string> External { get; init => field = [.. value]; } = [];

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            if (EntryPoints.Count == 0)
                throw new InvalidOperationException($"{nameof(EntryPoints)} must contain at least one entry point.");
            if (OutDir is not null && OutFile is not null)
                throw new InvalidOperationException($"Specify either {nameof(OutDir)} or {nameof(OutFile)}, but not both.");
            
            return
                [
                    "bun build",
                    Args(EntryPoints),
                    Arg("--production", Production),
                    Arg("--compile", Compile),
                    Arg("--target", Target),
                    Arg("--outdir", OutDir),
                    Arg("--outfile", OutFile),
                    Arg("--splitting", Splitting),
                    .. External.Select(value => Arg("--external", value)),
                    Arg("--format", Format),
                    Arg("--minify", Minify),
                ];
        }
    }
}

/// <summary>Bun build execution target.</summary>
public enum BunBuildTarget
{
    /// <summary>Web browsers.</summary>
    Browser,
    /// <summary>The Bun runtime.</summary>
    Bun,
    /// <summary>The Node.js runtime.</summary>
    Node,
}

/// <summary>Bun build module format.</summary>
public enum BunBuildFormat
{
    /// <summary>ECMAScript modules.</summary>
    Esm,
    /// <summary>CommonJS modules.</summary>
    Cjs,
    /// <summary>An immediately invoked function expression.</summary>
    Iife,
}

/// <summary>Publishes the current package or a prepared archive to an npm registry.</summary>
public sealed record BunPublish : BunCommand
{
    /// <summary>Archive or directory to publish; null publishes the current package.</summary>
    public string? Package { get; init; }
    /// <summary>Reports publish contents without uploading.</summary>
    public bool DryRun { get; init; }
    /// <summary>Registry visibility for scoped packages.</summary>
    public BunAccess? Access { get; init; }
    /// <summary>Distribution tag applied to the release.</summary>
    public string? Tag { get; init; }
    /// <summary>One-time password used for authentication.</summary>
    public string? Otp { get; init; }
    /// <summary>Succeeds when this version already exists.</summary>
    public bool TolerateRepublish { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "bun publish",
            Arg(Package),
            Arg("--dry-run", DryRun),
            ..OutputParts,
            Arg("--access", Access),
            Arg("--tag", Tag),
            Arg("--otp", Otp),
            Arg("--tolerate-republish", TolerateRepublish),
        ];
}

/// <summary>Registry visibility accepted by Bun publish.</summary>
public enum BunAccess
{
    /// <summary>Visible only to authorized users.</summary>
    Restricted,
    /// <summary>Publicly visible.</summary>
    Public,
}
