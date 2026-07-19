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
    protected DotNetTargetCommand() => Targets = [Do.Solution.Path];

    /// <summary>Supplies the values emitted by the <c>--target</c> option.</summary>
    public IReadOnlyList<string> Targets { get => GetArgumentArray("target"); init => SetArgumentArray("target", "", value); }
}

/// <summary>Base command definition for .NET CLI commands sharing build options.</summary>
public abstract record DotNetBuildOptionsCommand : DotNetTargetCommand
{
    /// <summary>Controls emission of the <c>--use-current-runtime</c> switch.</summary>
    public bool CurrentRuntime { get => GetFlag("use-current-runtime"); init => SetFlag("use-current-runtime", "--use-current-runtime", value); }
    /// <summary>Supplies the value emitted by the <c>--configuration</c> option.</summary>
    public string? Configuration { get => GetArgument("configuration"); init => SetArgument("configuration", "--configuration ", value); }
    /// <summary>Supplies the value emitted by the <c>--runtime</c> option.</summary>
    public string? Runtime { get => GetArgument("runtime"); init => SetArgument("runtime", "--runtime ", value); }
    /// <summary>Supplies the value emitted by the <c>--version-suffix</c> option.</summary>
    public string? VersionSuffix { get => GetArgument("version-suffix"); init => SetArgument("version-suffix", "--version-suffix ", value); }
    /// <summary>Controls emission of the <c>--no-restore</c> switch.</summary>
    public bool NoRestore { get => GetFlag("no-restore"); init => SetFlag("no-restore", "--no-restore", value); }
    /// <summary>Controls emission of the <c>--interactive</c> switch.</summary>
    public bool Interactive { get => GetFlag("interactive"); init => SetFlag("interactive", "--interactive", value); }
    /// <summary>Supplies the value emitted by the <c>--verbosity</c> option.</summary>
    public string? Verbosity { get => GetArgument("verbosity"); init => SetArgument("verbosity", "--verbosity ", value); }
    /// <summary>Supplies the value emitted by the <c>--output</c> option.</summary>
    public string? Output { get => GetArgument("output"); init => SetArgument("output", "--output ", value); }
    /// <summary>Supplies the value emitted by the <c>--artifacts-path</c> option.</summary>
    public string? ArtifactsPath { get => GetArgument("artifacts-path"); init => SetArgument("artifacts-path", "--artifacts-path ", value); }
    /// <summary>Controls emission of the <c>--nologo</c> switch.</summary>
    public bool NoLogo { get => GetFlag("nologo"); init => SetFlag("nologo", "--nologo", value); }
    /// <summary>Controls emission of the <c>--disable-build-servers</c> switch.</summary>
    public bool DisableBuildServers { get => GetFlag("disable-build-servers"); init => SetFlag("disable-build-servers", "--disable-build-servers", value); }
}

/// <summary>Builds a <c>dotnet build</c> command.</summary>
public sealed record DotNetBuild : DotNetBuildOptionsCommand
{
    /// <summary>Gets the executable and subcommand prefix rendered before configured options.</summary>
    protected override string CommandPrefix => "dotnet build";
    /// <summary>Supplies the value emitted by the <c>--framework</c> option.</summary>
    public string? Framework { get => GetArgument("framework"); init => SetArgument("framework", "--framework ", value); }
    /// <summary>Controls emission of the <c>--debug</c> switch.</summary>
    public bool Debug { get => GetFlag("debug"); init => SetFlag("debug", "--debug", value); }
    /// <summary>Controls emission of the <c>--no-incremental</c> switch.</summary>
    public bool NoIncremental { get => GetFlag("no-incremental"); init => SetFlag("no-incremental", "--no-incremental", value); }
    /// <summary>Controls emission of the <c>--no-dependencies</c> switch.</summary>
    public bool NoDependencies { get => GetFlag("no-dependencies"); init => SetFlag("no-dependencies", "--no-dependencies", value); }
    /// <summary>Controls emission of the <c>--self-contained</c> switch.</summary>
    public bool? SelfContained { get => GetFlag("self-contained", "--self-contained", "--no-self-contained"); init => SetFlag("self-contained", "--self-contained", "--no-self-contained", value); }
    /// <summary>Supplies the value emitted by the <c>--arch</c> option.</summary>
    public string? Architecture { get => GetArgument("arch"); init => SetArgument("arch", "--arch ", value); }
    /// <summary>Supplies the value emitted by the <c>--os</c> option.</summary>
    public string? OperatingSystem { get => GetArgument("os"); init => SetArgument("os", "--os ", value); }
}

/// <summary>Builds a <c>dotnet clean</c> command.</summary>
public sealed record DotNetClean : DotNetTargetCommand
{
    /// <summary>Gets the executable and subcommand prefix rendered before configured options.</summary>
    protected override string CommandPrefix => "dotnet clean";
    /// <summary>Supplies the value emitted by the <c>--framework</c> option.</summary>
    public string? Framework { get => GetArgument("framework"); init => SetArgument("framework", "--framework ", value); }
    /// <summary>Supplies the value emitted by the <c>--runtime</c> option.</summary>
    public string? Runtime { get => GetArgument("runtime"); init => SetArgument("runtime", "--runtime ", value); }
    /// <summary>Supplies the value emitted by the <c>--configuration</c> option.</summary>
    public string? Configuration { get => GetArgument("configuration"); init => SetArgument("configuration", "--configuration ", value); }
    /// <summary>Controls emission of the <c>--interactive</c> switch.</summary>
    public bool Interactive { get => GetFlag("interactive"); init => SetFlag("interactive", "--interactive", value); }
    /// <summary>Supplies the value emitted by the <c>--verbosity</c> option.</summary>
    public string? Verbosity { get => GetArgument("verbosity"); init => SetArgument("verbosity", "--verbosity ", value); }
    /// <summary>Supplies the value emitted by the <c>--output</c> option.</summary>
    public string? Output { get => GetArgument("output"); init => SetArgument("output", "--output ", value); }
    /// <summary>Supplies the value emitted by the <c>--artifacts-path</c> option.</summary>
    public string? ArtifactsPath { get => GetArgument("artifacts-path"); init => SetArgument("artifacts-path", "--artifacts-path ", value); }
    /// <summary>Controls emission of the <c>--nologo</c> switch.</summary>
    public bool NoLogo { get => GetFlag("nologo"); init => SetFlag("nologo", "--nologo", value); }
    /// <summary>Controls emission of the <c>--disable-build-servers</c> switch.</summary>
    public bool DisableBuildServers { get => GetFlag("disable-build-servers"); init => SetFlag("disable-build-servers", "--disable-build-servers", value); }
}

/// <summary>Builds a <c>dotnet dev-certs</c> command.</summary>
public sealed record DotNetDevCerts : ExecToolCommand
{
    /// <summary>Gets the executable and subcommand prefix rendered before configured options.</summary>
    protected override string CommandPrefix => "dotnet dev-certs https";
    /// <summary>Supplies the value emitted by the <c>--export-path</c> option.</summary>
    public string? ExportPath { get => GetArgument("export-path"); init => SetArgument("export-path", "--export-path ", value); }
    /// <summary>Supplies the value emitted by the <c>--password</c> option.</summary>
    public string? Password { get => GetArgument("password"); init => SetArgument("password", "--password ", value); }
    /// <summary>Controls emission of the <c>--no-password</c> switch.</summary>
    public bool NoPassword { get => GetFlag("no-password"); init => SetFlag("no-password", "--no-password", value); }
    /// <summary>Controls emission of the <c>--check</c> switch.</summary>
    public bool Check { get => GetFlag("check"); init => SetFlag("check", "--check", value); }
    /// <summary>Controls emission of the <c>--clean</c> switch.</summary>
    public bool Clean { get => GetFlag("clean"); init => SetFlag("clean", "--clean", value); }
    /// <summary>Supplies the value emitted by the <c>--import</c> option.</summary>
    public string? Import { get => GetArgument("import"); init => SetArgument("import", "--import ", value); }
    /// <summary>Supplies the value emitted by the <c>--format</c> option.</summary>
    public string? Format { get => GetArgument("format"); init => SetArgument("format", "--format ", value); }
    /// <summary>Controls emission of the <c>--trust</c> switch.</summary>
    public bool Trust { get => GetFlag("trust"); init => SetFlag("trust", "--trust", value); }
    /// <summary>Controls emission of the <c>--verbose</c> switch.</summary>
    public bool Verbose { get => GetFlag("verbose"); init => SetFlag("verbose", "--verbose", value); }
    /// <summary>Controls emission of the <c>--quiet</c> switch.</summary>
    public bool Quiet { get => GetFlag("quiet"); init => SetFlag("quiet", "--quiet", value); }
    /// <summary>Controls emission of the <c>--check-trust-machine-readable</c> switch.</summary>
    public bool CheckTrustMachineReadable { get => GetFlag("check-trust-machine-readable"); init => SetFlag("check-trust-machine-readable", "--check-trust-machine-readable", value); }
}

/// <summary>Builds a <c>dotnet pack</c> command.</summary>
public sealed record DotNetPack : DotNetBuildOptionsCommand
{
    /// <summary>Gets the executable and subcommand prefix rendered before configured options.</summary>
    protected override string CommandPrefix => "dotnet pack";
    /// <summary>Controls emission of the <c>--no-build</c> switch.</summary>
    public bool NoBuild { get => GetFlag("no-build"); init => SetFlag("no-build", "--no-build", value); }
    /// <summary>Controls emission of the <c>--include-symbols</c> switch.</summary>
    public bool IncludeSymbols { get => GetFlag("include-symbols"); init => SetFlag("include-symbols", "--include-symbols", value); }
    /// <summary>Controls emission of the <c>--include-source</c> switch.</summary>
    public bool IncludeSource { get => GetFlag("include-source"); init => SetFlag("include-source", "--include-source", value); }
    /// <summary>Controls emission of the <c>--serviceable</c> switch.</summary>
    public bool Serviceable { get => GetFlag("serviceable"); init => SetFlag("serviceable", "--serviceable", value); }
    /// <summary>Supplies the value emitted by the <c>--version</c> option.</summary>
    public string? Version { get => GetArgument("version"); init => SetArgument("version", "--version ", value); }
}

/// <summary>Builds a <c>dotnet nuget push</c> command.</summary>
public sealed record DotNetNuGetPush : ExecToolCommand
{
    /// <summary>Gets the executable and subcommand prefix rendered before configured options.</summary>
    protected override string CommandPrefix => "dotnet nuget push";
    /// <summary>Supplies the package path to push.</summary>
    public string? Package { get => GetArgument("package"); init => SetArgument("package", value); }
    /// <summary>Allows connections to package sources using HTTP.</summary>
    public bool AllowInsecureConnections { get => GetFlag("allow-insecure-connections"); init => SetFlag("allow-insecure-connections", "--allow-insecure-connections", value); }
    /// <summary>Disables buffering when pushing to an HTTP(S) server.</summary>
    public bool DisableBuffering { get => GetFlag("disable-buffering"); init => SetFlag("disable-buffering", "--disable-buffering", value); }
    /// <summary>Forces invariant English output.</summary>
    public bool ForceEnglishOutput { get => GetFlag("force-english-output"); init => SetFlag("force-english-output", "--force-english-output", value); }
    /// <summary>Allows the command to wait for interactive authentication or input.</summary>
    public bool Interactive { get => GetFlag("interactive"); init => SetFlag("interactive", "--interactive", value); }
    /// <summary>Supplies the API key for the package source.</summary>
    public string? ApiKey { get => GetArgument("api-key"); init => SetArgument("api-key", "--api-key ", value); }
    /// <summary>Prevents symbol packages from being pushed.</summary>
    public bool NoSymbols { get => GetFlag("no-symbols"); init => SetFlag("no-symbols", "--no-symbols", value); }
    /// <summary>Prevents <c>api/v2/package</c> from being appended to the source URL.</summary>
    public bool NoServiceEndpoint { get => GetFlag("no-service-endpoint"); init => SetFlag("no-service-endpoint", "--no-service-endpoint", value); }
    /// <summary>Supplies the package source URL.</summary>
    public string? Source { get => GetArgument("source"); init => SetArgument("source", "--source ", value); }
    /// <summary>Skips packages whose version already exists at the source.</summary>
    public bool SkipDuplicate { get => GetFlag("skip-duplicate"); init => SetFlag("skip-duplicate", "--skip-duplicate", value); }
    /// <summary>Supplies the API key for the symbol source.</summary>
    public string? SymbolApiKey { get => GetArgument("symbol-api-key"); init => SetArgument("symbol-api-key", "--symbol-api-key ", value); }
    /// <summary>Supplies the symbol server URL.</summary>
    public string? SymbolSource { get => GetArgument("symbol-source"); init => SetArgument("symbol-source", "--symbol-source ", value); }

    /// <summary>Supplies the push timeout in seconds.</summary>
    public TimeSpan? Timeout
    {
        get => GetInt("timeout") is { } seconds ? TimeSpan.FromSeconds(seconds) : null;
        init => SetInt("timeout", "--timeout ", value is { } timeout ? (int)timeout.TotalSeconds : null);
    }
    /// <summary>Supplies the NuGet configuration file.</summary>
    public string? ConfigFile { get => GetArgument("configfile"); init => SetArgument("configfile", "--configfile ", value); }
}

/// <summary>Builds a <c>dotnet restore</c> command.</summary>
public sealed record DotNetRestore : DotNetTargetCommand
{
    /// <summary>Gets the executable and subcommand prefix rendered before configured options.</summary>
    protected override string CommandPrefix => "dotnet restore";
    /// <summary>Controls emission of the <c>--disable-build-servers</c> switch.</summary>
    public bool DisableBuildServers { get => GetFlag("disable-build-servers"); init => SetFlag("disable-build-servers", "--disable-build-servers", value); }
    /// <summary>Supplies the values emitted by the <c>--source</c> option.</summary>
    public IReadOnlyList<string> Sources { get => GetArgumentArray("source"); init => SetArgumentArray("source", "--source ", value, " --source "); }
    /// <summary>Supplies the value emitted by the <c>--packages</c> option.</summary>
    public string? Packages { get => GetArgument("packages"); init => SetArgument("packages", "--packages ", value); }
    /// <summary>Controls emission of the <c>--use-current-runtime</c> switch.</summary>
    public bool CurrentRuntime { get => GetFlag("use-current-runtime"); init => SetFlag("use-current-runtime", "--use-current-runtime", value); }
    /// <summary>Controls emission of the <c>--disable-parallel</c> switch.</summary>
    public bool DisableParallel { get => GetFlag("disable-parallel"); init => SetFlag("disable-parallel", "--disable-parallel", value); }
    /// <summary>Supplies the value emitted by the <c>--configfile</c> option.</summary>
    public string? ConfigFile { get => GetArgument("configfile"); init => SetArgument("configfile", "--configfile ", value); }
    /// <summary>Controls emission of the <c>--no-http-cache</c> switch.</summary>
    public bool NoHttpCache { get => GetFlag("no-http-cache"); init => SetFlag("no-http-cache", "--no-http-cache", value); }
    /// <summary>Controls emission of the <c>--ignore-failed-sources</c> switch.</summary>
    public bool IgnoreFailedSources { get => GetFlag("ignore-failed-sources"); init => SetFlag("ignore-failed-sources", "--ignore-failed-sources", value); }
    /// <summary>Controls emission of the <c>--force</c> switch.</summary>
    public bool Force { get => GetFlag("force"); init => SetFlag("force", "--force", value); }
    /// <summary>Supplies the value emitted by the <c>--runtime</c> option.</summary>
    public string? Runtime { get => GetArgument("runtime"); init => SetArgument("runtime", "--runtime ", value); }
    /// <summary>Controls emission of the <c>--no-dependencies</c> switch.</summary>
    public bool NoDependencies { get => GetFlag("no-dependencies"); init => SetFlag("no-dependencies", "--no-dependencies", value); }
    /// <summary>Supplies the value emitted by the <c>--verbosity</c> option.</summary>
    public string? Verbosity { get => GetArgument("verbosity"); init => SetArgument("verbosity", "--verbosity ", value); }
    /// <summary>Controls emission of the <c>--interactive</c> switch.</summary>
    public bool Interactive { get => GetFlag("interactive"); init => SetFlag("interactive", "--interactive", value); }
    /// <summary>Supplies the value emitted by the <c>--artifacts-path</c> option.</summary>
    public string? ArtifactsPath { get => GetArgument("artifacts-path"); init => SetArgument("artifacts-path", "--artifacts-path ", value); }
    /// <summary>Controls emission of the <c>--use-lock-file</c> switch.</summary>
    public bool UseLockFile { get => GetFlag("use-lock-file"); init => SetFlag("use-lock-file", "--use-lock-file", value); }
    /// <summary>Controls emission of the <c>--locked-mode</c> switch.</summary>
    public bool LockedMode { get => GetFlag("locked-mode"); init => SetFlag("locked-mode", "--locked-mode", value); }
    /// <summary>Supplies the value emitted by the <c>--lock-file-path</c> option.</summary>
    public string? LockFilePath { get => GetArgument("lock-file-path"); init => SetArgument("lock-file-path", "--lock-file-path ", value); }
    /// <summary>Controls emission of the <c>--force-evaluate</c> switch.</summary>
    public bool ForceEvaluate { get => GetFlag("force-evaluate"); init => SetFlag("force-evaluate", "--force-evaluate", value); }
    /// <summary>Supplies the value emitted by the <c>--arch</c> option.</summary>
    public string? Architecture { get => GetArgument("arch"); init => SetArgument("arch", "--arch ", value); }
    /// <summary>Supplies the value emitted by the <c>--os</c> option.</summary>
    public string? OperatingSystem { get => GetArgument("os"); init => SetArgument("os", "--os ", value); }
}

/// <summary>Builds a <c>dotnet test</c> command.</summary>
public sealed record DotNetTest : DotNetTargetCommand
{
    /// <summary>Gets the executable and subcommand prefix rendered before configured options.</summary>
    protected override string CommandPrefix => "dotnet test";
    /// <summary>Supplies the value emitted by the <c>--settings</c> option.</summary>
    public string? Settings { get => GetArgument("settings"); init => SetArgument("settings", "--settings ", value); }
    /// <summary>Controls emission of the <c>--list-tests</c> switch.</summary>
    public bool ListTests { get => GetFlag("list-tests"); init => SetFlag("list-tests", "--list-tests", value); }
    /// <summary>Supplies the values emitted by the <c>--environment</c> option.</summary>
    public IReadOnlyList<string> Environment { get => GetArgumentArray("environment"); init => SetArgumentArray("environment", "--environment ", value, " --environment "); }
    /// <summary>Supplies the value emitted by the <c>--filter</c> option.</summary>
    public string? Filter { get => GetArgument("filter"); init => SetArgument("filter", "--filter ", value); }
    /// <summary>Supplies the value emitted by the <c>--test-adapter-path</c> option.</summary>
    public string? TestAdapterPath { get => GetArgument("test-adapter-path"); init => SetArgument("test-adapter-path", "--test-adapter-path ", value); }
    /// <summary>Supplies the values emitted by the <c>--logger</c> option.</summary>
    public IReadOnlyList<string> Loggers { get => GetArgumentArray("logger"); init => SetArgumentArray("logger", "--logger ", value, " --logger "); }
    /// <summary>Supplies the value emitted by the <c>--output</c> option.</summary>
    public string? Output { get => GetArgument("output"); init => SetArgument("output", "--output ", value); }
    /// <summary>Supplies the value emitted by the <c>--artifacts-path</c> option.</summary>
    public string? ArtifactsPath { get => GetArgument("artifacts-path"); init => SetArgument("artifacts-path", "--artifacts-path ", value); }
    /// <summary>Supplies the value emitted by the <c>--diag</c> option.</summary>
    public string? Diag { get => GetArgument("diag"); init => SetArgument("diag", "--diag ", value); }
    /// <summary>Controls emission of the <c>--no-build</c> switch.</summary>
    public bool NoBuild { get => GetFlag("no-build"); init => SetFlag("no-build", "--no-build", value); }
    /// <summary>Supplies the value emitted by the <c>--results-directory</c> option.</summary>
    public string? ResultsDirectory { get => GetArgument("results-directory"); init => SetArgument("results-directory", "--results-directory ", value); }
    /// <summary>Supplies the value emitted by the <c>--collect</c> option.</summary>
    public string? Collect { get => GetArgument("collect"); init => SetArgument("collect", "--collect ", value); }
    /// <summary>Controls emission of the <c>--blame</c> switch.</summary>
    public bool Blame { get => GetFlag("blame"); init => SetFlag("blame", "--blame", value); }
    /// <summary>Controls emission of the <c>--blame-crash</c> switch.</summary>
    public bool BlameCrash { get => GetFlag("blame-crash"); init => SetFlag("blame-crash", "--blame-crash", value); }
    /// <summary>Supplies the value emitted by the <c>--blame-crash-dump-type</c> option.</summary>
    public string? BlameCrashDumpType { get => GetArgument("blame-crash-dump-type"); init => SetArgument("blame-crash-dump-type", "--blame-crash-dump-type ", value); }
    /// <summary>Controls emission of the <c>--blame-crash-collect-always</c> switch.</summary>
    public bool BlameCrashCollectAlways { get => GetFlag("blame-crash-collect-always"); init => SetFlag("blame-crash-collect-always", "--blame-crash-collect-always", value); }
    /// <summary>Controls emission of the <c>--blame-hang</c> switch.</summary>
    public bool BlameHang { get => GetFlag("blame-hang"); init => SetFlag("blame-hang", "--blame-hang", value); }
    /// <summary>Supplies the value emitted by the <c>--blame-hang-dump-type</c> option.</summary>
    public string? BlameHangDumpType { get => GetArgument("blame-hang-dump-type"); init => SetArgument("blame-hang-dump-type", "--blame-hang-dump-type ", value); }
    /// <summary>Supplies the value emitted by the <c>--blame-hang-timeout</c> option.</summary>
    public string? BlameHangTimeout { get => GetArgument("blame-hang-timeout"); init => SetArgument("blame-hang-timeout", "--blame-hang-timeout ", value); }
    /// <summary>Controls emission of the <c>--nologo</c> switch.</summary>
    public bool NoLogo { get => GetFlag("nologo"); init => SetFlag("nologo", "--nologo", value); }
    /// <summary>Supplies the value emitted by the <c>--configuration</c> option.</summary>
    public string? Configuration { get => GetArgument("configuration"); init => SetArgument("configuration", "--configuration ", value); }
    /// <summary>Supplies the value emitted by the <c>--framework</c> option.</summary>
    public string? Framework { get => GetArgument("framework"); init => SetArgument("framework", "--framework ", value); }
    /// <summary>Supplies the value emitted by the <c>--runtime</c> option.</summary>
    public string? Runtime { get => GetArgument("runtime"); init => SetArgument("runtime", "--runtime ", value); }
    /// <summary>Controls emission of the <c>--no-restore</c> switch.</summary>
    public bool NoRestore { get => GetFlag("no-restore"); init => SetFlag("no-restore", "--no-restore", value); }
    /// <summary>Controls emission of the <c>--interactive</c> switch.</summary>
    public bool Interactive { get => GetFlag("interactive"); init => SetFlag("interactive", "--interactive", value); }
    /// <summary>Supplies the value emitted by the <c>--verbosity</c> option.</summary>
    public string? Verbosity { get => GetArgument("verbosity"); init => SetArgument("verbosity", "--verbosity ", value); }
    /// <summary>Supplies the value emitted by the <c>--arch</c> option.</summary>
    public string? Architecture { get => GetArgument("arch"); init => SetArgument("arch", "--arch ", value); }
    /// <summary>Supplies the value emitted by the <c>--os</c> option.</summary>
    public string? OperatingSystem { get => GetArgument("os"); init => SetArgument("os", "--os ", value); }
    /// <summary>Controls emission of the <c>--disable-build-servers</c> switch.</summary>
    public bool DisableBuildServers { get => GetFlag("disable-build-servers"); init => SetFlag("disable-build-servers", "--disable-build-servers", value); }
}

/// <summary>Restores the .NET local tools in scope for the execution directory.</summary>
public sealed record DotNetToolRestore : ExecToolCommand
{
    /// <summary>The executable and subcommand prefix.</summary>
    protected override string CommandPrefix => "dotnet tool restore";
    /// <summary>The NuGet configuration file used exclusively for restore.</summary>
    public string? ConfigFile { get => GetArgument("configfile"); init => SetArgument("configfile", "--configfile ", value); }
    /// <summary>Additional NuGet package sources.</summary>
    public IReadOnlyList<string> AddSources { get => GetArgumentArray("add-source"); init => SetArgumentArray("add-source", "--add-source ", value, " --add-source "); }
    /// <summary>An explicit local tool manifest path.</summary>
    public string? ToolManifest { get => GetArgument("tool-manifest"); init => SetArgument("tool-manifest", "--tool-manifest ", value); }
    /// <summary>Whether parallel project restore is disabled.</summary>
    public bool DisableParallel { get => GetFlag("disable-parallel"); init => SetFlag("disable-parallel", "--disable-parallel", value); }
    /// <summary>Whether unavailable package sources are treated as warnings.</summary>
    public bool IgnoreFailedSources { get => GetFlag("ignore-failed-sources"); init => SetFlag("ignore-failed-sources", "--ignore-failed-sources", value); }
    /// <summary>Whether NuGet caches are bypassed.</summary>
    public bool NoCache { get => GetFlag("no-cache"); init => SetFlag("no-cache", "--no-cache", value); }
    /// <summary>Whether restore may wait for interactive authentication or input.</summary>
    public bool Interactive { get => GetFlag("interactive"); init => SetFlag("interactive", "--interactive", value); }
    /// <summary>The restore logging verbosity.</summary>
    public string? Verbosity { get => GetArgument("verbosity"); init => SetArgument("verbosity", "--verbosity ", value); }
}

/// <summary>Builds a <c>dotnet watch</c> command.</summary>
public sealed record DotNetWatch : ExecToolCommand
{
    /// <summary>Gets the executable and subcommand prefix rendered before configured options.</summary>
    protected override string CommandPrefix => "dotnet watch";
    /// <summary>Controls emission of the <c>--quiet</c> switch.</summary>
    public bool Quiet { get => GetFlag("quiet"); init => SetFlag("quiet", "--quiet", value); }
    /// <summary>Controls emission of the <c>--verbose</c> switch.</summary>
    public bool Verbose { get => GetFlag("verbose"); init => SetFlag("verbose", "--verbose", value); }
    /// <summary>Controls emission of the <c>--list</c> switch.</summary>
    public bool List { get => GetFlag("list"); init => SetFlag("list", "--list", value); }
    /// <summary>Controls emission of the <c>--no-hot-reload</c> switch.</summary>
    public bool NoHotReload { get => GetFlag("no-hot-reload"); init => SetFlag("no-hot-reload", "--no-hot-reload", value); }
    /// <summary>Controls emission of the <c>--non-interactive</c> switch.</summary>
    public bool NonInteractive { get => GetFlag("non-interactive"); init => SetFlag("non-interactive", "--non-interactive", value); }
    /// <summary>Supplies the value emitted by the <c>--configuration</c> option.</summary>
    public string? Configuration { get => GetArgument("configuration"); init => SetArgument("configuration", "--configuration ", value); }
    /// <summary>Supplies the value emitted by the <c>--framework</c> option.</summary>
    public string? Framework { get => GetArgument("framework"); init => SetArgument("framework", "--framework ", value); }
    /// <summary>Supplies the value emitted by the <c>--runtime</c> option.</summary>
    public string? Runtime { get => GetArgument("runtime"); init => SetArgument("runtime", "--runtime ", value); }
    /// <summary>Controls emission of the <c>--interactive</c> switch.</summary>
    public bool Interactive { get => GetFlag("interactive"); init => SetFlag("interactive", "--interactive", value); }
    /// <summary>Controls emission of the <c>--no-restore</c> switch.</summary>
    public bool NoRestore { get => GetFlag("no-restore"); init => SetFlag("no-restore", "--no-restore", value); }
    /// <summary>Controls emission of the <c>--self-contained</c> switch.</summary>
    public bool? SelfContained { get => GetFlag("self-contained", "--self-contained", "--no-self-contained"); init => SetFlag("self-contained", "--self-contained", "--no-self-contained", value); }
    /// <summary>Supplies the value emitted by the <c>--verbosity</c> option.</summary>
    public string? Verbosity { get => GetArgument("verbosity"); init => SetArgument("verbosity", "--verbosity ", value); }
    /// <summary>Supplies the value emitted by the <c>--arch</c> option.</summary>
    public string? Architecture { get => GetArgument("arch"); init => SetArgument("arch", "--arch ", value); }
    /// <summary>Supplies the value emitted by the <c>--os</c> option.</summary>
    public string? OperatingSystem { get => GetArgument("os"); init => SetArgument("os", "--os ", value); }
    /// <summary>Controls emission of the <c>--disable-build-servers</c> switch.</summary>
    public bool DisableBuildServers { get => GetFlag("disable-build-servers"); init => SetFlag("disable-build-servers", "--disable-build-servers", value); }
    /// <summary>Supplies the value emitted by the <c>--artifacts-path</c> option.</summary>
    public string? ArtifactsPath { get => GetArgument("artifacts-path"); init => SetArgument("artifacts-path", "--artifacts-path ", value); }
}

/// <summary>Builds a <c>dotnet format</c> command.</summary>
public sealed record DotNetFormat : DotNetTargetCommand
{
    /// <summary>Gets the executable and subcommand prefix rendered before configured options.</summary>
    protected override string CommandPrefix => "dotnet format";
    /// <summary>Selects which formatting category the command processes.</summary>
    public FormatCommand? Command { get => GetEnum<FormatCommand>("command"); init => SetEnum("command", value); }
    /// <summary>Supplies the value emitted by the <c>--command</c> option.</summary>
    public string? CustomCommand { get => GetArgument("command"); init => SetArgument("command", value); }
    /// <summary>Supplies the values emitted by the <c>--diagnostics</c> option.</summary>
    public IReadOnlyList<string> Diagnostics { get => GetArgumentArray("diagnostics"); init => SetArgumentArray("diagnostics", "--diagnostics ", value); }
    /// <summary>Supplies the values emitted by the <c>--exclude-diagnostics</c> option.</summary>
    public IReadOnlyList<string> ExcludeDiagnostics { get => GetArgumentArray("exclude-diagnostics"); init => SetArgumentArray("exclude-diagnostics", "--exclude-diagnostics ", value); }
    /// <summary>Supplies the value emitted by the <c>--severity</c> option.</summary>
    public string? Severity { get => GetArgument("severity"); init => SetArgument("severity", "--severity ", value); }
    /// <summary>Controls emission of the <c>--no-restore</c> switch.</summary>
    public bool NoRestore { get => GetFlag("no-restore"); init => SetFlag("no-restore", "--no-restore", value); }
    /// <summary>Controls emission of the <c>--verify-no-changes</c> switch.</summary>
    public bool VerifyNoChanges { get => GetFlag("verify-no-changes"); init => SetFlag("verify-no-changes", "--verify-no-changes", value); }
    /// <summary>Supplies the values emitted by the <c>--include</c> option.</summary>
    public IReadOnlyList<string> Include { get => GetArgumentArray("include"); init => SetArgumentArray("include", "--include ", value); }
    /// <summary>Supplies the values emitted by the <c>--exclude</c> option.</summary>
    public IReadOnlyList<string> Exclude { get => GetArgumentArray("exclude"); init => SetArgumentArray("exclude", "--exclude ", value); }
    /// <summary>Controls emission of the <c>--include-generated</c> switch.</summary>
    public bool IncludeGenerated { get => GetFlag("include-generated"); init => SetFlag("include-generated", "--include-generated", value); }
    /// <summary>Supplies the value emitted by the <c>--verbosity</c> option.</summary>
    public string? Verbosity { get => GetArgument("verbosity"); init => SetArgument("verbosity", "--verbosity ", value); }
    /// <summary>Supplies the value emitted by the <c>--binarylog</c> option.</summary>
    public string? BinaryLog { get => GetArgument("binarylog"); init => SetArgument("binarylog", "--binarylog ", value); }
    /// <summary>Supplies the value emitted by the <c>--report</c> option.</summary>
    public string? Report { get => GetArgument("report"); init => SetArgument("report", "--report ", value); }
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
