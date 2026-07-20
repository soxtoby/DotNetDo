using Serilog.Events;

namespace DotNetDo;

/// <summary>Provides strongly typed definitions for supported command-line tools.</summary>
public static partial class Tools
{
    /// <summary>Provides command definitions for the .NET CLI.</summary>
    public static class DotNet
    {
        /// <summary>Creates a new <see cref="DotNetBuild"/> command definition.</summary>
        public static DotNetBuild Build => new();
        /// <summary>Creates a new <see cref="DotNetClean"/> command definition.</summary>
        public static DotNetClean Clean => new();
        /// <summary>Creates a new <see cref="DotNetDevCerts"/> command definition.</summary>
        public static DotNetDevCerts DevCerts => new();
        /// <summary>Creates a new <see cref="DotNetFormat"/> command definition.</summary>
        public static DotNetFormat Format => new();
        /// <summary>Creates a new <see cref="DotNetPack"/> command definition.</summary>
        public static DotNetPack Pack => new();
        /// <summary>Creates a new <see cref="DotNetNuGetPush"/> command definition.</summary>
        public static DotNetNuGetPush NuGetPush => new();
        /// <summary>Creates a new <see cref="DotNetRestore"/> command definition.</summary>
        public static DotNetRestore Restore => new();
        /// <summary>Creates a new <see cref="DotNetTest"/> command definition.</summary>
        public static DotNetTest Test => new();
        /// <summary>Creates a new <see cref="DotNetToolRestore"/> command definition.</summary>
        public static DotNetToolRestore ToolRestore => new();
        /// <summary>Creates a new <see cref="DotNetWatch"/> command definition.</summary>
        public static DotNetWatch Watch => new();
    }
}

/// <summary>Base command definition for .NET CLI commands that accept project or solution targets.</summary>
public abstract record DotNetTargetCommand : ExecToolCommand
{
    /// <summary>Dot net target command.</summary>
    protected DotNetTargetCommand()
    {
        Targets = [Do.Solution.Path];
        Verbosity = MSBuildOutputVolume.From(Logging.Level).ToString().ToLowerInvariant();
    }

    /// <summary>Supplies the values emitted by the <c>--target</c> option.</summary>
    public IReadOnlyList<string> Targets { get; init => field = value.ToArray(); } = [];
    /// <summary>Supplies the value emitted by the <c>--verbosity</c> option.</summary>
    public string? Verbosity { get; init; }

    /// <summary>Gets the canonically ordered target arguments shared by derived commands.</summary>
    protected IReadOnlyList<string?> TargetParts => [Args(Targets), Arg("--verbosity", Verbosity)];
}

/// <summary>Base command definition for .NET CLI commands sharing build options.</summary>
public abstract record DotNetBuildOptionsCommand : DotNetTargetCommand
{
    /// <summary>Controls emission of the <c>--use-current-runtime</c> switch.</summary>
    public bool CurrentRuntime { get; init; }
    /// <summary>Supplies the value emitted by the <c>--configuration</c> option.</summary>
    public string? Configuration { get; init; }
    /// <summary>Supplies the value emitted by the <c>--runtime</c> option.</summary>
    public string? Runtime { get; init; }
    /// <summary>Supplies the value emitted by the <c>--version-suffix</c> option.</summary>
    public string? VersionSuffix { get; init; }
    /// <summary>Controls emission of the <c>--no-restore</c> switch.</summary>
    public bool NoRestore { get; init; }
    /// <summary>Controls emission of the <c>--interactive</c> switch.</summary>
    public bool Interactive { get; init; }
    /// <summary>Supplies the value emitted by the <c>--output</c> option.</summary>
    public string? Output { get; init; }
    /// <summary>Supplies the value emitted by the <c>--artifacts-path</c> option.</summary>
    public string? ArtifactsPath { get; init; }
    /// <summary>Controls emission of the <c>--nologo</c> switch.</summary>
    public bool NoLogo { get; init; }
    /// <summary>Controls emission of the <c>--disable-build-servers</c> switch.</summary>
    public bool DisableBuildServers { get; init; }

    /// <summary>Gets the canonically ordered build arguments shared by derived commands.</summary>
    protected IReadOnlyList<string?> BuildParts =>
        [
            ..TargetParts,
            Arg("--use-current-runtime", CurrentRuntime),
            Arg("--configuration", Configuration),
            Arg("--runtime", Runtime),
            Arg("--version-suffix", VersionSuffix),
            Arg("--no-restore", NoRestore),
            Arg("--interactive", Interactive),
            Arg("--output", Output),
            Arg("--artifacts-path", ArtifactsPath),
            Arg("--nologo", NoLogo),
            Arg("--disable-build-servers", DisableBuildServers),
        ];
}

/// <summary>Builds a <c>dotnet build</c> command.</summary>
public sealed record DotNetBuild : DotNetBuildOptionsCommand
{
    /// <summary>Supplies the value emitted by the <c>--framework</c> option.</summary>
    public string? Framework { get; init; }
    /// <summary>Controls emission of the <c>--debug</c> switch.</summary>
    public bool Debug { get; init; }
    /// <summary>Controls emission of the <c>--no-incremental</c> switch.</summary>
    public bool NoIncremental { get; init; }
    /// <summary>Controls emission of the <c>--no-dependencies</c> switch.</summary>
    public bool NoDependencies { get; init; }
    /// <summary>Controls emission of the <c>--self-contained</c> switch.</summary>
    public bool? SelfContained { get; init; }
    /// <summary>Supplies the value emitted by the <c>--arch</c> option.</summary>
    public string? Architecture { get; init; }
    /// <summary>Supplies the value emitted by the <c>--os</c> option.</summary>
    public string? OperatingSystem { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "dotnet build",
            ..BuildParts,
            Arg("--framework", Framework),
            Arg("--debug", Debug),
            Arg("--no-incremental", NoIncremental),
            Arg("--no-dependencies", NoDependencies),
            Arg("--self-contained", "--no-self-contained", SelfContained),
            Arg("--arch", Architecture),
            Arg("--os", OperatingSystem),
        ];
}

/// <summary>Builds a <c>dotnet clean</c> command.</summary>
public sealed record DotNetClean : DotNetTargetCommand
{
    /// <summary>Supplies the value emitted by the <c>--framework</c> option.</summary>
    public string? Framework { get; init; }
    /// <summary>Supplies the value emitted by the <c>--runtime</c> option.</summary>
    public string? Runtime { get; init; }
    /// <summary>Supplies the value emitted by the <c>--configuration</c> option.</summary>
    public string? Configuration { get; init; }
    /// <summary>Controls emission of the <c>--interactive</c> switch.</summary>
    public bool Interactive { get; init; }
    /// <summary>Supplies the value emitted by the <c>--output</c> option.</summary>
    public string? Output { get; init; }
    /// <summary>Supplies the value emitted by the <c>--artifacts-path</c> option.</summary>
    public string? ArtifactsPath { get; init; }
    /// <summary>Controls emission of the <c>--nologo</c> switch.</summary>
    public bool NoLogo { get; init; }
    /// <summary>Controls emission of the <c>--disable-build-servers</c> switch.</summary>
    public bool DisableBuildServers { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "dotnet clean",
            ..TargetParts,
            Arg("--framework", Framework),
            Arg("--runtime", Runtime),
            Arg("--configuration", Configuration),
            Arg("--interactive", Interactive),
            Arg("--output", Output),
            Arg("--artifacts-path", ArtifactsPath),
            Arg("--nologo", NoLogo),
            Arg("--disable-build-servers", DisableBuildServers),
        ];
}

/// <summary>Builds a <c>dotnet dev-certs</c> command.</summary>
public sealed record DotNetDevCerts : ExecToolCommand
{
    /// <summary>Creates a command with output volume derived from the current logging level.</summary>
    public DotNetDevCerts()
    {
        (Quiet, Verbose) = DotNetOutputVolume.From(Logging.Level);
    }

    /// <summary>Supplies the value emitted by the <c>--export-path</c> option.</summary>
    public string? ExportPath { get; init; }
    /// <summary>Supplies the value emitted by the <c>--password</c> option.</summary>
    public string? Password { get; init; }
    /// <summary>Controls emission of the <c>--no-password</c> switch.</summary>
    public bool NoPassword { get; init; }
    /// <summary>Controls emission of the <c>--check</c> switch.</summary>
    public bool Check { get; init; }
    /// <summary>Controls emission of the <c>--clean</c> switch.</summary>
    public bool Clean { get; init; }
    /// <summary>Supplies the value emitted by the <c>--import</c> option.</summary>
    public string? Import { get; init; }
    /// <summary>Supplies the value emitted by the <c>--format</c> option.</summary>
    public string? Format { get; init; }
    /// <summary>Controls emission of the <c>--trust</c> switch.</summary>
    public bool Trust { get; init; }
    /// <summary>Controls emission of the <c>--verbose</c> switch.</summary>
    public bool Verbose { get; init; }
    /// <summary>Controls emission of the <c>--quiet</c> switch.</summary>
    public bool Quiet { get; init; }
    /// <summary>Controls emission of the <c>--check-trust-machine-readable</c> switch.</summary>
    public bool CheckTrustMachineReadable { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "dotnet dev-certs https",
            Arg("--quiet", Quiet),
            Arg("--verbose", Verbose),
            Arg("--export-path", ExportPath),
            Arg("--password", Password),
            Arg("--no-password", NoPassword),
            Arg("--check", Check),
            Arg("--clean", Clean),
            Arg("--import", Import),
            Arg("--format", Format),
            Arg("--trust", Trust),
            Arg("--check-trust-machine-readable", CheckTrustMachineReadable),
        ];
}

/// <summary>Builds a <c>dotnet pack</c> command.</summary>
public sealed record DotNetPack : DotNetBuildOptionsCommand
{
    /// <summary>Controls emission of the <c>--no-build</c> switch.</summary>
    public bool NoBuild { get; init; }
    /// <summary>Controls emission of the <c>--include-symbols</c> switch.</summary>
    public bool IncludeSymbols { get; init; }
    /// <summary>Controls emission of the <c>--include-source</c> switch.</summary>
    public bool IncludeSource { get; init; }
    /// <summary>Controls emission of the <c>--serviceable</c> switch.</summary>
    public bool Serviceable { get; init; }
    /// <summary>Supplies the value emitted by the <c>--version</c> option.</summary>
    public string? Version { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "dotnet pack",
            ..BuildParts,
            Arg("--no-build", NoBuild),
            Arg("--include-symbols", IncludeSymbols),
            Arg("--include-source", IncludeSource),
            Arg("--serviceable", Serviceable),
            Arg("--version", Version),
        ];
}

/// <summary>Builds a <c>dotnet nuget push</c> command.</summary>
public sealed record DotNetNuGetPush : ExecToolCommand
{
    /// <summary>Supplies the package path to push.</summary>
    public string? Package { get; init; }
    /// <summary>Allows connections to package sources using HTTP.</summary>
    public bool AllowInsecureConnections { get; init; }
    /// <summary>Disables buffering when pushing to an HTTP(S) server.</summary>
    public bool DisableBuffering { get; init; }
    /// <summary>Forces invariant English output.</summary>
    public bool ForceEnglishOutput { get; init; }
    /// <summary>Allows the command to wait for interactive authentication or input.</summary>
    public bool Interactive { get; init; }
    /// <summary>Supplies the API key for the package source.</summary>
    public string? ApiKey { get; init; }
    /// <summary>Prevents symbol packages from being pushed.</summary>
    public bool NoSymbols { get; init; }
    /// <summary>Prevents <c>api/v2/package</c> from being appended to the source URL.</summary>
    public bool NoServiceEndpoint { get; init; }
    /// <summary>Supplies the package source URL.</summary>
    public string? Source { get; init; }
    /// <summary>Skips packages whose version already exists at the source.</summary>
    public bool SkipDuplicate { get; init; }
    /// <summary>Supplies the API key for the symbol source.</summary>
    public string? SymbolApiKey { get; init; }
    /// <summary>Supplies the symbol server URL.</summary>
    public string? SymbolSource { get; init; }

    /// <summary>Supplies the push timeout in seconds.</summary>
    public TimeSpan? Timeout { get; init; }
    /// <summary>Supplies the NuGet configuration file.</summary>
    public string? ConfigFile { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "dotnet nuget push",
            Arg(Package),
            Arg("--allow-insecure-connections", AllowInsecureConnections),
            Arg("--disable-buffering", DisableBuffering),
            Arg("--force-english-output", ForceEnglishOutput),
            Arg("--interactive", Interactive),
            Arg("--source", Source),
            Arg("--api-key", ApiKey),
            Arg("--no-symbols", NoSymbols),
            Arg("--no-service-endpoint", NoServiceEndpoint),
            Arg("--skip-duplicate", SkipDuplicate),
            Arg("--symbol-api-key", SymbolApiKey),
            Arg("--symbol-source", SymbolSource),
            Arg("--timeout", (int?)Timeout?.TotalSeconds),
            Arg("--configfile", ConfigFile),
        ];
}

/// <summary>Builds a <c>dotnet restore</c> command.</summary>
public sealed record DotNetRestore : DotNetTargetCommand
{
    /// <summary>Controls emission of the <c>--disable-build-servers</c> switch.</summary>
    public bool DisableBuildServers { get; init; }
    /// <summary>Supplies the values emitted by the <c>--source</c> option.</summary>
    public IReadOnlyList<string> Sources { get; init => field = value.ToArray(); } = [];
    /// <summary>Supplies the value emitted by the <c>--packages</c> option.</summary>
    public string? Packages { get; init; }
    /// <summary>Controls emission of the <c>--use-current-runtime</c> switch.</summary>
    public bool CurrentRuntime { get; init; }
    /// <summary>Controls emission of the <c>--disable-parallel</c> switch.</summary>
    public bool DisableParallel { get; init; }
    /// <summary>Supplies the value emitted by the <c>--configfile</c> option.</summary>
    public string? ConfigFile { get; init; }
    /// <summary>Controls emission of the <c>--no-http-cache</c> switch.</summary>
    public bool NoHttpCache { get; init; }
    /// <summary>Controls emission of the <c>--ignore-failed-sources</c> switch.</summary>
    public bool IgnoreFailedSources { get; init; }
    /// <summary>Controls emission of the <c>--force</c> switch.</summary>
    public bool Force { get; init; }
    /// <summary>Supplies the value emitted by the <c>--runtime</c> option.</summary>
    public string? Runtime { get; init; }
    /// <summary>Controls emission of the <c>--no-dependencies</c> switch.</summary>
    public bool NoDependencies { get; init; }
    /// <summary>Controls emission of the <c>--interactive</c> switch.</summary>
    public bool Interactive { get; init; }
    /// <summary>Supplies the value emitted by the <c>--artifacts-path</c> option.</summary>
    public string? ArtifactsPath { get; init; }
    /// <summary>Controls emission of the <c>--use-lock-file</c> switch.</summary>
    public bool UseLockFile { get; init; }
    /// <summary>Controls emission of the <c>--locked-mode</c> switch.</summary>
    public bool LockedMode { get; init; }
    /// <summary>Supplies the value emitted by the <c>--lock-file-path</c> option.</summary>
    public string? LockFilePath { get; init; }
    /// <summary>Controls emission of the <c>--force-evaluate</c> switch.</summary>
    public bool ForceEvaluate { get; init; }
    /// <summary>Supplies the value emitted by the <c>--arch</c> option.</summary>
    public string? Architecture { get; init; }
    /// <summary>Supplies the value emitted by the <c>--os</c> option.</summary>
    public string? OperatingSystem { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "dotnet restore",
            ..TargetParts,
            Arg("--disable-build-servers", DisableBuildServers),
            Args("--source", Sources, " --source "),
            Arg("--packages", Packages),
            Arg("--use-current-runtime", CurrentRuntime),
            Arg("--disable-parallel", DisableParallel),
            Arg("--configfile", ConfigFile),
            Arg("--no-http-cache", NoHttpCache),
            Arg("--ignore-failed-sources", IgnoreFailedSources),
            Arg("--force", Force),
            Arg("--runtime", Runtime),
            Arg("--no-dependencies", NoDependencies),
            Arg("--interactive", Interactive),
            Arg("--artifacts-path", ArtifactsPath),
            Arg("--use-lock-file", UseLockFile),
            Arg("--locked-mode", LockedMode),
            Arg("--lock-file-path", LockFilePath),
            Arg("--force-evaluate", ForceEvaluate),
            Arg("--arch", Architecture),
            Arg("--os", OperatingSystem),
        ];
}

/// <summary>Builds a <c>dotnet test</c> command.</summary>
public sealed record DotNetTest : DotNetTargetCommand
{
    /// <summary>Supplies the value emitted by the <c>--settings</c> option.</summary>
    public string? Settings { get; init; }
    /// <summary>Controls emission of the <c>--list-tests</c> switch.</summary>
    public bool ListTests { get; init; }
    /// <summary>Supplies the values emitted by the <c>--environment</c> option.</summary>
    public IReadOnlyList<string> Environment { get; init => field = value.ToArray(); } = [];
    /// <summary>Supplies the value emitted by the <c>--filter</c> option.</summary>
    public string? Filter { get; init; }
    /// <summary>Supplies the value emitted by the <c>--test-adapter-path</c> option.</summary>
    public string? TestAdapterPath { get; init; }
    /// <summary>Supplies the values emitted by the <c>--logger</c> option.</summary>
    public IReadOnlyList<string> Loggers { get; init => field = value.ToArray(); } = [];
    /// <summary>Supplies the value emitted by the <c>--output</c> option.</summary>
    public string? Output { get; init; }
    /// <summary>Supplies the value emitted by the <c>--artifacts-path</c> option.</summary>
    public string? ArtifactsPath { get; init; }
    /// <summary>Supplies the value emitted by the <c>--diag</c> option.</summary>
    public string? Diag { get; init; }
    /// <summary>Controls emission of the <c>--no-build</c> switch.</summary>
    public bool NoBuild { get; init; }
    /// <summary>Supplies the value emitted by the <c>--results-directory</c> option.</summary>
    public string? ResultsDirectory { get; init; }
    /// <summary>Supplies the value emitted by the <c>--collect</c> option.</summary>
    public string? Collect { get; init; }
    /// <summary>Controls emission of the <c>--blame</c> switch.</summary>
    public bool Blame { get; init; }
    /// <summary>Controls emission of the <c>--blame-crash</c> switch.</summary>
    public bool BlameCrash { get; init; }
    /// <summary>Supplies the value emitted by the <c>--blame-crash-dump-type</c> option.</summary>
    public string? BlameCrashDumpType { get; init; }
    /// <summary>Controls emission of the <c>--blame-crash-collect-always</c> switch.</summary>
    public bool BlameCrashCollectAlways { get; init; }
    /// <summary>Controls emission of the <c>--blame-hang</c> switch.</summary>
    public bool BlameHang { get; init; }
    /// <summary>Supplies the value emitted by the <c>--blame-hang-dump-type</c> option.</summary>
    public string? BlameHangDumpType { get; init; }
    /// <summary>Supplies the value emitted by the <c>--blame-hang-timeout</c> option.</summary>
    public string? BlameHangTimeout { get; init; }
    /// <summary>Controls emission of the <c>--nologo</c> switch.</summary>
    public bool NoLogo { get; init; }
    /// <summary>Supplies the value emitted by the <c>--configuration</c> option.</summary>
    public string? Configuration { get; init; }
    /// <summary>Supplies the value emitted by the <c>--framework</c> option.</summary>
    public string? Framework { get; init; }
    /// <summary>Supplies the value emitted by the <c>--runtime</c> option.</summary>
    public string? Runtime { get; init; }
    /// <summary>Controls emission of the <c>--no-restore</c> switch.</summary>
    public bool NoRestore { get; init; }
    /// <summary>Controls emission of the <c>--interactive</c> switch.</summary>
    public bool Interactive { get; init; }
    /// <summary>Supplies the value emitted by the <c>--arch</c> option.</summary>
    public string? Architecture { get; init; }
    /// <summary>Supplies the value emitted by the <c>--os</c> option.</summary>
    public string? OperatingSystem { get; init; }
    /// <summary>Controls emission of the <c>--disable-build-servers</c> switch.</summary>
    public bool DisableBuildServers { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "dotnet test",
            ..TargetParts,
            Arg("--settings", Settings),
            Arg("--list-tests", ListTests),
            Args("--environment", Environment, " --environment "),
            Arg("--filter", Filter),
            Arg("--test-adapter-path", TestAdapterPath),
            Args("--logger", Loggers, " --logger "),
            Arg("--output", Output),
            Arg("--artifacts-path", ArtifactsPath),
            Arg("--diag", Diag),
            Arg("--no-build", NoBuild),
            Arg("--results-directory", ResultsDirectory),
            Arg("--collect", Collect),
            Arg("--blame", Blame),
            Arg("--blame-crash", BlameCrash),
            Arg("--blame-crash-dump-type", BlameCrashDumpType),
            Arg("--blame-crash-collect-always", BlameCrashCollectAlways),
            Arg("--blame-hang", BlameHang),
            Arg("--blame-hang-dump-type", BlameHangDumpType),
            Arg("--blame-hang-timeout", BlameHangTimeout),
            Arg("--nologo", NoLogo),
            Arg("--configuration", Configuration),
            Arg("--framework", Framework),
            Arg("--runtime", Runtime),
            Arg("--no-restore", NoRestore),
            Arg("--interactive", Interactive),
            Arg("--arch", Architecture),
            Arg("--os", OperatingSystem),
            Arg("--disable-build-servers", DisableBuildServers),
        ];
}

/// <summary>Restores the .NET local tools in scope for the execution directory.</summary>
public sealed record DotNetToolRestore : ExecToolCommand
{
    /// <summary>Creates a command with output volume derived from the current logging level.</summary>
    public DotNetToolRestore() => Verbosity = MSBuildOutputVolume.From(Logging.Level).ToString().ToLowerInvariant();
    /// <summary>The NuGet configuration file used exclusively for restore.</summary>
    public string? ConfigFile { get; init; }
    /// <summary>Additional NuGet package sources.</summary>
    public IReadOnlyList<string> AddSources { get; init => field = value.ToArray(); } = [];
    /// <summary>An explicit local tool manifest path.</summary>
    public string? ToolManifest { get; init; }
    /// <summary>Whether parallel project restore is disabled.</summary>
    public bool DisableParallel { get; init; }
    /// <summary>Whether unavailable package sources are treated as warnings.</summary>
    public bool IgnoreFailedSources { get; init; }
    /// <summary>Whether NuGet caches are bypassed.</summary>
    public bool NoCache { get; init; }
    /// <summary>Whether restore may wait for interactive authentication or input.</summary>
    public bool Interactive { get; init; }
    /// <summary>The restore logging verbosity.</summary>
    public string? Verbosity { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "dotnet tool restore",
            Arg("--verbosity", Verbosity),
            Arg("--configfile", ConfigFile),
            Args("--add-source", AddSources, " --add-source "),
            Arg("--tool-manifest", ToolManifest),
            Arg("--disable-parallel", DisableParallel),
            Arg("--ignore-failed-sources", IgnoreFailedSources),
            Arg("--no-cache", NoCache),
            Arg("--interactive", Interactive),
        ];
}

/// <summary>Builds a <c>dotnet watch</c> command.</summary>
public sealed record DotNetWatch : ExecToolCommand
{
    /// <summary>Creates a command with output volume derived from the current logging level.</summary>
    public DotNetWatch()
    {
        (Quiet, Verbose) = DotNetOutputVolume.From(Logging.Level);
        Verbosity = MSBuildOutputVolume.From(Logging.Level).ToString().ToLowerInvariant();
    }
    /// <summary>Controls emission of the <c>--quiet</c> switch.</summary>
    public bool Quiet { get; init; }
    /// <summary>Controls emission of the <c>--verbose</c> switch.</summary>
    public bool Verbose { get; init; }
    /// <summary>Controls emission of the <c>--list</c> switch.</summary>
    public bool List { get; init; }
    /// <summary>Controls emission of the <c>--no-hot-reload</c> switch.</summary>
    public bool NoHotReload { get; init; }
    /// <summary>Controls emission of the <c>--non-interactive</c> switch.</summary>
    public bool NonInteractive { get; init; }
    /// <summary>Supplies the value emitted by the <c>--configuration</c> option.</summary>
    public string? Configuration { get; init; }
    /// <summary>Supplies the value emitted by the <c>--framework</c> option.</summary>
    public string? Framework { get; init; }
    /// <summary>Supplies the value emitted by the <c>--runtime</c> option.</summary>
    public string? Runtime { get; init; }
    /// <summary>Controls emission of the <c>--interactive</c> switch.</summary>
    public bool Interactive { get; init; }
    /// <summary>Controls emission of the <c>--no-restore</c> switch.</summary>
    public bool NoRestore { get; init; }
    /// <summary>Controls emission of the <c>--self-contained</c> switch.</summary>
    public bool? SelfContained { get; init; }
    /// <summary>Supplies the value emitted by the <c>--verbosity</c> option.</summary>
    public string? Verbosity { get; init; }
    /// <summary>Supplies the value emitted by the <c>--arch</c> option.</summary>
    public string? Architecture { get; init; }
    /// <summary>Supplies the value emitted by the <c>--os</c> option.</summary>
    public string? OperatingSystem { get; init; }
    /// <summary>Controls emission of the <c>--disable-build-servers</c> switch.</summary>
    public bool DisableBuildServers { get; init; }
    /// <summary>Supplies the value emitted by the <c>--artifacts-path</c> option.</summary>
    public string? ArtifactsPath { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "dotnet watch",
            Arg("--quiet", Quiet),
            Arg("--verbose", Verbose),
            Arg("--verbosity", Verbosity),
            Arg("--list", List),
            Arg("--no-hot-reload", NoHotReload),
            Arg("--non-interactive", NonInteractive),
            Arg("--configuration", Configuration),
            Arg("--framework", Framework),
            Arg("--runtime", Runtime),
            Arg("--interactive", Interactive),
            Arg("--no-restore", NoRestore),
            Arg("--self-contained", "--no-self-contained", SelfContained),
            Arg("--arch", Architecture),
            Arg("--os", OperatingSystem),
            Arg("--disable-build-servers", DisableBuildServers),
            Arg("--artifacts-path", ArtifactsPath),
        ];
}

/// <summary>Builds a <c>dotnet format</c> command.</summary>
public sealed record DotNetFormat : DotNetTargetCommand
{
    /// <summary>Selects which formatting category the command processes.</summary>
    public FormatCommand? Command { get; init; }
    /// <summary>Supplies the value emitted by the <c>--command</c> option.</summary>
    public string? CustomCommand { get; init; }
    /// <summary>Supplies the values emitted by the <c>--diagnostics</c> option.</summary>
    public IReadOnlyList<string> Diagnostics { get; init => field = value.ToArray(); } = [];
    /// <summary>Supplies the values emitted by the <c>--exclude-diagnostics</c> option.</summary>
    public IReadOnlyList<string> ExcludeDiagnostics { get; init => field = value.ToArray(); } = [];
    /// <summary>Supplies the value emitted by the <c>--severity</c> option.</summary>
    public string? Severity { get; init; }
    /// <summary>Controls emission of the <c>--no-restore</c> switch.</summary>
    public bool NoRestore { get; init; }
    /// <summary>Controls emission of the <c>--verify-no-changes</c> switch.</summary>
    public bool VerifyNoChanges { get; init; }
    /// <summary>Supplies the values emitted by the <c>--include</c> option.</summary>
    public IReadOnlyList<string> Include { get; init => field = value.ToArray(); } = [];
    /// <summary>Supplies the values emitted by the <c>--exclude</c> option.</summary>
    public IReadOnlyList<string> Exclude { get; init => field = value.ToArray(); } = [];
    /// <summary>Controls emission of the <c>--include-generated</c> switch.</summary>
    public bool IncludeGenerated { get; init; }
    /// <summary>Supplies the value emitted by the <c>--binarylog</c> option.</summary>
    public string? BinaryLog { get; init; }
    /// <summary>Supplies the value emitted by the <c>--report</c> option.</summary>
    public string? Report { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "dotnet format",
            Arg(CustomCommand ?? Command?.ToString().ToLowerInvariant()),
            ..TargetParts,
            Args("--diagnostics", Diagnostics),
            Args("--exclude-diagnostics", ExcludeDiagnostics),
            Arg("--severity", Severity),
            Arg("--no-restore", NoRestore),
            Arg("--verify-no-changes", VerifyNoChanges),
            Args("--include", Include),
            Args("--exclude", Exclude),
            Arg("--include-generated", IncludeGenerated),
            Arg("--binarylog", BinaryLog),
            Arg("--report", Report),
        ];
}

/// <summary>Identifies a supported <c>dotnet format</c> subcommand.</summary>
public enum FormatCommand
{
    /// <summary>Formats whitespace.</summary>
    Whitespace,
    /// <summary>Formats code style.</summary>
    Style,
    /// <summary>Applies analyzer fixes.</summary>
    Analyzers,
}

static class DotNetOutputVolume
{
    public static (bool Quiet, bool Verbose) From(LogEventLevel level) =>
        (level >= LogEventLevel.Warning, level <= LogEventLevel.Debug);
}
