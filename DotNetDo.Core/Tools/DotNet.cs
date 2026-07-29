using Serilog.Events;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetDo;

/// <summary>Exposes supported external tools as immutable, directly executable command values.</summary>
public static partial class Tools
{
    /// <summary>Builds, restores, tests, packs, formats, watches, and manages tools through the .NET CLI.</summary>
    public static class DotNet
    {
        /// <summary>Compiles the workspace solution or selected projects and their dependencies.</summary>
        public static DotNetBuild Build => new();
        /// <summary>Removes build outputs produced for the workspace solution or selected projects.</summary>
        public static DotNetClean Clean => new();
        /// <summary>Creates, checks, trusts, imports, exports, or removes HTTPS development certificates.</summary>
        public static DotNetDevCerts DevCerts => new();
        /// <summary>Applies or verifies whitespace, style, and analyzer formatting across a workspace.</summary>
        public static DotNetFormat Format => new();
        /// <summary>Builds projects and produces NuGet packages.</summary>
        public static DotNetPack Pack => new();
        /// <summary>Searches configured NuGet feeds and returns structured package results.</summary>
        public static DotNetPackageSearch PackageSearch => new();
        /// <summary>Uploads NuGet packages and optional symbol packages to a package source.</summary>
        public static DotNetNuGetPush NuGetPush => new();
        /// <summary>Resolves project dependencies and writes restore assets for the workspace.</summary>
        public static DotNetRestore Restore => new();
        /// <summary>Builds and runs tests in the workspace solution or selected projects.</summary>
        public static DotNetTest Test => new();
        /// <summary>Installs local tools declared by the applicable tool manifest.</summary>
        public static DotNetToolRestore ToolRestore => new();
        /// <summary>Updates a global, local-manifest, or explicit-path .NET tool package.</summary>
        public static DotNetToolUpdate ToolUpdate => new();
        /// <summary>Rebuilds or hot-reloads an application when watched source files change.</summary>
        public static DotNetWatch Watch => new();
    }
}

/// <summary>Defaults target-based .NET operations to the workspace solution and logging-derived verbosity.</summary>
public abstract record DotNetTargetCommand : ExecToolCommand
{
    /// <summary>Defaults target-based commands to the workspace solution and maps logging level to MSBuild verbosity.</summary>
    protected DotNetTargetCommand()
    {
        Targets = [Do.Solution.Path];
        Verbosity = MSBuildOutputVolume.From(Logging.Level).ToString().ToLowerInvariant();
    }

    /// <summary>Projects or solutions operated on; defaults to the discovered workspace solution.</summary>
    public IReadOnlyList<string> Targets { get; init => field = value.ToArray(); } = [];
    /// <summary>MSBuild output detail; defaults from <see cref="Logging.Level"/>.</summary>
    public string? Verbosity { get; init; }

    /// <summary>Places targets before logging verbosity for all target-based .NET operations.</summary>
    protected IReadOnlyList<string?> TargetParts => [Args(Targets), Arg("--verbosity", Verbosity)];
}

/// <summary>Shares build configuration, restore, runtime, output, and build-server behavior across build and pack.</summary>
public abstract record DotNetBuildOptionsCommand : DotNetTargetCommand
{
    /// <summary>Creates a command with defaults for the current build locality.</summary>
    protected DotNetBuildOptionsCommand() => Configuration = MSBuildDefaults.Configuration;

    /// <summary>Uses the current runtime as the target runtime instead of resolving one from the project.</summary>
    public bool CurrentRuntime { get; init; }
    /// <summary>The build configuration; defaults to <c>Debug</c> locally and <c>Release</c> in CI.</summary>
    public string? Configuration { get; init; }
    /// <summary>The target runtime identifier, such as <c>win-x64</c>.</summary>
    public string? Runtime { get; init; }
    /// <summary>Replaces the project's version suffix when forming the build version.</summary>
    public string? VersionSuffix { get; init; }
    /// <summary>Skips implicit restore; assets must already be current.</summary>
    public bool NoRestore { get; init; }
    /// <summary>Allows authentication and other restore operations to prompt for input.</summary>
    public bool Interactive { get; init; }
    /// <summary>Places all build outputs in this directory instead of project-defined locations.</summary>
    public string? Output { get; init; }
    /// <summary>Places outputs for all projects beneath this artifacts root, separated by project.</summary>
    public string? ArtifactsPath { get; init; }
    /// <summary>Suppresses the startup banner and copyright message.</summary>
    public bool NoLogo { get; init; }
    /// <summary>Prevents reuse of persistent build servers during this invocation.</summary>
    public bool DisableBuildServers { get; init; }

    /// <summary>Places shared target and build behavior before operation-specific arguments.</summary>
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

/// <summary>Compiles selected projects and their dependencies, restoring first unless disabled.</summary>
public sealed record DotNetBuild : DotNetBuildOptionsCommand
{
    /// <summary>Builds only the specified target framework, which must exist in the project.</summary>
    public string? Framework { get; init; }
    /// <summary>Enables additional CLI debug diagnostics.</summary>
    public bool Debug { get; init; }
    /// <summary>Forces a clean dependency graph evaluation instead of an incremental build.</summary>
    public bool NoIncremental { get; init; }
    /// <summary>Builds the selected project without building project references.</summary>
    public bool NoDependencies { get; init; }
    /// <summary>Controls whether the .NET runtime is bundled; <see langword="null"/> leaves project defaults unchanged.</summary>
    public bool? SelfContained { get; init; }
    /// <summary>Shorthand target architecture combined with the default runtime identifier.</summary>
    public string? Architecture { get; init; }
    /// <summary>Shorthand target operating system combined with the default runtime identifier.</summary>
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

/// <summary>Removes build outputs for selected projects, framework, runtime, and configuration.</summary>
public sealed record DotNetClean : DotNetTargetCommand
{
    /// <summary>Creates a command with defaults for the current build locality.</summary>
    public DotNetClean() => Configuration = MSBuildDefaults.Configuration;

    /// <summary>Selects one target framework declared by the project.</summary>
    public string? Framework { get; init; }
    /// <summary>Targets the specified runtime identifier, such as <c>win-x64</c>.</summary>
    public string? Runtime { get; init; }
    /// <summary>Selects the named build configuration.</summary>
    public string? Configuration { get; init; }
    /// <summary>Allows authentication and other operations to prompt for input.</summary>
    public bool Interactive { get; init; }
    /// <summary>Places command outputs in the specified directory.</summary>
    public string? Output { get; init; }
    /// <summary>Places outputs for all projects beneath this artifacts root, separated by project.</summary>
    public string? ArtifactsPath { get; init; }
    /// <summary>Suppresses the startup banner and copyright message.</summary>
    public bool NoLogo { get; init; }
    /// <summary>Prevents reuse of persistent build servers during this invocation.</summary>
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

/// <summary>Manages the local HTTPS development certificate used by ASP.NET Core.</summary>
public sealed record DotNetDevCerts : ExecToolCommand
{
    /// <summary>Creates a command with output volume derived from the current logging level.</summary>
    public DotNetDevCerts()
    {
        (Quiet, Verbose) = DotNetOutputVolume.From(Logging.Level);
    }

    /// <summary>Exports the HTTPS development certificate to this file; the extension determines the default format.</summary>
    public string? ExportPath { get; init; }
    /// <summary>Protects the exported certificate with this password; requires an export path.</summary>
    public string? Password { get; init; }
    /// <summary>Exports a certificate without password protection; requires PEM format and cannot be combined with a password.</summary>
    public bool NoPassword { get; init; }
    /// <summary>Checks whether a valid HTTPS development certificate exists without creating one.</summary>
    public bool Check { get; init; }
    /// <summary>Removes HTTPS development certificates from the local certificate store.</summary>
    public bool Clean { get; init; }
    /// <summary>Clears other HTTPS development certificates, then imports this certificate into the machine store.</summary>
    public string? Import { get; init; }
    /// <summary>Selects the exported certificate format, <c>Pfx</c> or <c>Pem</c>.</summary>
    public string? Format { get; init; }
    /// <summary>Trusts the selected HTTPS development certificate when supported by the platform.</summary>
    public bool Trust { get; init; }
    /// <summary>Writes diagnostic detail beyond normal command output.</summary>
    public bool Verbose { get; init; }
    /// <summary>Suppresses nonessential command output.</summary>
    public bool Quiet { get; init; }
    /// <summary>Checks for a trusted certificate and reports the result as JSON without changing the store.</summary>
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

/// <summary>Builds selected projects and produces NuGet packages from their package metadata.</summary>
public sealed record DotNetPack : DotNetBuildOptionsCommand
{
    /// <summary>Skips building before the operation; required outputs must already exist.</summary>
    public bool NoBuild { get; init; }
    /// <summary>Creates an additional symbols package alongside the main NuGet package.</summary>
    public bool IncludeSymbols { get; init; }
    /// <summary>Includes source files in the symbols package and implies symbol-package creation.</summary>
    public bool IncludeSource { get; init; }
    /// <summary>Marks the produced package as serviceable in its project properties.</summary>
    public bool Serviceable { get; init; }
    /// <summary>Sets the package version for this pack invocation.</summary>
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

/// <summary>Uploads a NuGet package and, unless disabled, its matching symbol package.</summary>
public sealed record DotNetNuGetPush : ExecToolCommand
{
    /// <summary>The <c>.nupkg</c> file to upload; glob patterns are accepted by the .NET CLI.</summary>
    public string? Package { get; init; }
    /// <summary>Allows connections to package sources using HTTP.</summary>
    public bool AllowInsecureConnections { get; init; }
    /// <summary>Disables buffering when pushing to an HTTP(S) server.</summary>
    public bool DisableBuffering { get; init; }
    /// <summary>Forces invariant English output.</summary>
    public bool ForceEnglishOutput { get; init; }
    /// <summary>Allows the command to wait for interactive authentication or input.</summary>
    public bool Interactive { get; init; }
    /// <summary>The credential sent to the package source; use a <see cref="Secret"/> value to keep it redacted.</summary>
    public string? ApiKey { get; init; }
    /// <summary>Prevents symbol packages from being pushed.</summary>
    public bool NoSymbols { get; init; }
    /// <summary>Prevents <c>api/v2/package</c> from being appended to the source URL.</summary>
    public bool NoServiceEndpoint { get; init; }
    /// <summary>The source name, path, or URL receiving the package; defaults to NuGet configuration.</summary>
    public string? Source { get; init; }
    /// <summary>Skips packages whose version already exists at the source.</summary>
    public bool SkipDuplicate { get; init; }
    /// <summary>The credential sent when uploading the symbol package.</summary>
    public string? SymbolApiKey { get; init; }
    /// <summary>The source name, path, or URL receiving the symbol package.</summary>
    public string? SymbolSource { get; init; }

    /// <summary>Maximum duration allowed for a server push; rendered as whole seconds.</summary>
    public TimeSpan? Timeout { get; init; }
    /// <summary>Uses this NuGet configuration file instead of the configuration hierarchy.</summary>
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

/// <summary>Resolves NuGet dependencies and writes the assets required by later build operations.</summary>
public sealed record DotNetRestore : DotNetTargetCommand
{
    /// <summary>Prevents reuse of persistent build servers during restore.</summary>
    public bool DisableBuildServers { get; init; }
    /// <summary>Package sources used instead of those configured in NuGet configuration files.</summary>
    public IReadOnlyList<string> Sources { get; init => field = value.ToArray(); } = [];
    /// <summary>Directory in which restored packages are installed.</summary>
    public string? Packages { get; init; }
    /// <summary>Uses the current runtime as a restore target in addition to project-declared runtimes.</summary>
    public bool CurrentRuntime { get; init; }
    /// <summary>Restores projects sequentially instead of concurrently.</summary>
    public bool DisableParallel { get; init; }
    /// <summary>Uses only this NuGet configuration file instead of the configuration hierarchy.</summary>
    public string? ConfigFile { get; init; }
    /// <summary>Bypasses cached HTTP responses and downloads package metadata again.</summary>
    public bool NoHttpCache { get; init; }
    /// <summary>Treats unavailable sources as warnings when required packages are available elsewhere.</summary>
    public bool IgnoreFailedSources { get; init; }
    /// <summary>Re-resolves every dependency even when the existing assets file is current.</summary>
    public bool Force { get; init; }
    /// <summary>Restores packages for this runtime identifier in addition to project-declared runtimes.</summary>
    public string? Runtime { get; init; }
    /// <summary>Restores the selected project without restoring project references.</summary>
    public bool NoDependencies { get; init; }
    /// <summary>Allows authentication providers and other restore operations to prompt for input.</summary>
    public bool Interactive { get; init; }
    /// <summary>Places restore outputs for all projects beneath this artifacts root.</summary>
    public string? ArtifactsPath { get; init; }
    /// <summary>Generates or updates a dependency lock file during restore.</summary>
    public bool UseLockFile { get; init; }
    /// <summary>Fails when restore would change the existing dependency lock file.</summary>
    public bool LockedMode { get; init; }
    /// <summary>Writes the lock file to this project-relative path instead of <c>packages.lock.json</c>.</summary>
    public string? LockFilePath { get; init; }
    /// <summary>Re-evaluates dependencies and updates the lock file even when it is otherwise current.</summary>
    public bool ForceEvaluate { get; init; }
    /// <summary>Shorthand restore architecture combined with the default runtime identifier.</summary>
    public string? Architecture { get; init; }
    /// <summary>Shorthand restore operating system combined with the default runtime identifier.</summary>
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

/// <summary>Builds selected test projects and runs their tests through the configured test platform.</summary>
public sealed record DotNetTest : DotNetTargetCommand
{
    /// <summary>Creates a command with defaults for the current build locality.</summary>
    public DotNetTest() => Configuration = MSBuildDefaults.Configuration;

    /// <summary>Uses this run-settings file to configure the test run.</summary>
    public string? Settings { get; init; }
    /// <summary>Discovers and lists tests without executing them.</summary>
    public bool ListTests { get; init; }
    /// <summary>Sets test-host environment variables as <c>NAME=VALUE</c>; specifying any value runs tests in an isolated process.</summary>
    public IReadOnlyList<string> Environment { get; init => field = value.ToArray(); } = [];
    /// <summary>Runs only tests matching the VSTest filter expression.</summary>
    public string? Filter { get; init; }
    /// <summary>Searches this directory for additional test adapters.</summary>
    public string? TestAdapterPath { get; init; }
    /// <summary>Enables test loggers; each value may include semicolon-delimited logger settings.</summary>
    public IReadOnlyList<string> Loggers { get; init => field = value.ToArray(); } = [];
    /// <summary>Places command outputs in the specified directory.</summary>
    public string? Output { get; init; }
    /// <summary>Places outputs for all projects beneath this artifacts root, separated by project.</summary>
    public string? ArtifactsPath { get; init; }
    /// <summary>Writes diagnostic test-platform logs to this file.</summary>
    public string? Diag { get; init; }
    /// <summary>Skips building before the operation; required outputs must already exist.</summary>
    public bool NoBuild { get; init; }
    /// <summary>Places test results and generated artifacts in this directory.</summary>
    public string? ResultsDirectory { get; init; }
    /// <summary>Enables the named data collector; collector settings may follow after a semicolon.</summary>
    public string? Collect { get; init; }
    /// <summary>Collects a sequence file identifying tests running near a crash or hang.</summary>
    public bool Blame { get; init; }
    /// <summary>Collects a process dump when the test host crashes.</summary>
    public bool BlameCrash { get; init; }
    /// <summary>Selects <c>mini</c> or <c>full</c> crash dumps; requires crash blame.</summary>
    public string? BlameCrashDumpType { get; init; }
    /// <summary>Collects a crash dump even when the test host exits normally; requires crash blame.</summary>
    public bool BlameCrashCollectAlways { get; init; }
    /// <summary>Terminates and dumps a test host when a test exceeds the configured hang timeout.</summary>
    public bool BlameHang { get; init; }
    /// <summary>Selects <c>mini</c>, <c>full</c>, or <c>none</c> for hang dumps; requires hang blame.</summary>
    public string? BlameHangDumpType { get; init; }
    /// <summary>Sets the per-test hang timeout using a value such as <c>90s</c>, <c>2m</c>, or <c>1h</c>.</summary>
    public string? BlameHangTimeout { get; init; }
    /// <summary>Suppresses the startup banner and copyright message.</summary>
    public bool NoLogo { get; init; }
    /// <summary>Selects the named build configuration.</summary>
    public string? Configuration { get; init; }
    /// <summary>Selects one target framework declared by the project.</summary>
    public string? Framework { get; init; }
    /// <summary>Targets the specified runtime identifier, such as <c>win-x64</c>.</summary>
    public string? Runtime { get; init; }
    /// <summary>Skips implicit restore; assets must already be current.</summary>
    public bool NoRestore { get; init; }
    /// <summary>Allows authentication and other operations to prompt for input.</summary>
    public bool Interactive { get; init; }
    /// <summary>Shorthand target architecture combined with the default runtime identifier.</summary>
    public string? Architecture { get; init; }
    /// <summary>Shorthand target operating system combined with the default runtime identifier.</summary>
    public string? OperatingSystem { get; init; }
    /// <summary>Prevents reuse of persistent build servers during this invocation.</summary>
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

/// <summary>Searches configured NuGet package sources and returns structured JSON results.</summary>
public sealed record DotNetPackageSearch : ToolCommand<DotNetPackageSearchResult>
{
    /// <summary>The package search term.</summary>
    public string? SearchTerm { get; init; }
    /// <summary>Package sources to search instead of the configured sources.</summary>
    public IReadOnlyList<string> Sources { get; init => field = value.ToArray(); } = [];
    /// <summary>Whether the package ID must exactly match the search term.</summary>
    public bool ExactMatch { get; init; }
    /// <summary>The maximum number of packages returned by each source.</summary>
    public int? Take { get; init; }
    /// <summary>The number of packages skipped by each source.</summary>
    public int? Skip { get; init; }
    /// <summary>Whether prerelease packages are included.</summary>
    public bool Prerelease { get; init; }
    /// <summary>Whether authentication may prompt interactively.</summary>
    public bool Interactive { get; init; }
    /// <summary>An explicit NuGet configuration file.</summary>
    public string? ConfigFile { get; init; }
    /// <summary>The command logging verbosity.</summary>
    public string? Verbosity { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "dotnet package search",
            Arg(SearchTerm),
            Args("--source", Sources, " --source "),
            Arg("--take", Take),
            Arg("--skip", Skip),
            Arg("--exact-match", ExactMatch),
            Arg("--interactive", Interactive),
            Arg("--prerelease", Prerelease),
            Arg("--configfile", ConfigFile),
            "--format json",
            Arg("--verbosity", Verbosity),
        ];

    /// <inheritdoc />
    protected override DotNetPackageSearchResult ReadResult(ExecResult result) =>
        DotNetPackageSearchResult.Parse(result);
}

/// <summary>The structured result emitted by <c>dotnet package search</c>.</summary>
public sealed record DotNetPackageSearchResult
{
    /// <summary>The output schema version.</summary>
    [JsonPropertyName("version")]
    public int Version { get; init; }
    /// <summary>Problems reported while searching sources.</summary>
    [JsonPropertyName("problems")]
    public IReadOnlyList<JsonElement> Problems { get; init; } = [];
    /// <summary>Results grouped by package source.</summary>
    [JsonPropertyName("searchResult")]
    public IReadOnlyList<DotNetPackageSearchSource> Sources { get; init; } = [];

    internal static DotNetPackageSearchResult Parse(ExecResult result) =>
        result.ReadJson<DotNetPackageSearchResult>()
        ?? throw new JsonException("dotnet package search returned no JSON object.");
}

/// <summary>Package-search results from one NuGet source.</summary>
public sealed record DotNetPackageSearchSource
{
    /// <summary>The configured source name.</summary>
    [JsonPropertyName("sourceName")]
    public string? Name { get; init; }
    /// <summary>Packages returned by the source.</summary>
    [JsonPropertyName("packages")]
    public IReadOnlyList<DotNetPackageSearchPackage> Packages { get; init; } = [];
}

/// <summary>A package returned by <c>dotnet package search</c>.</summary>
public sealed record DotNetPackageSearchPackage
{
    /// <summary>The package ID.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }
    /// <summary>The package version returned by an exact-match search.</summary>
    [JsonPropertyName("version")]
    public string? Version { get; init; }
    /// <summary>The latest eligible version reported by the source.</summary>
    [JsonPropertyName("latestVersion")]
    public string? LatestVersion { get; init; }
}

/// <summary>Updates a global, local-manifest, or explicit-path .NET tool.</summary>
public sealed record DotNetToolUpdate : ExecToolCommand
{
    /// <summary>The tool package ID.</summary>
    public string? Package { get; init; }
    /// <summary>Whether the global tool installation is updated.</summary>
    public bool Global { get; init; }
    /// <summary>Whether the local tool manifest is updated.</summary>
    public bool Local { get; init; }
    /// <summary>An explicit tool installation directory.</summary>
    public string? ToolPath { get; init; }
    /// <summary>An explicit package version.</summary>
    public string? Version { get; init; }
    /// <summary>An explicit NuGet configuration file.</summary>
    public string? ConfigFile { get; init; }
    /// <summary>An explicit local tool manifest.</summary>
    public string? ToolManifest { get; init; }
    /// <summary>Additional NuGet package sources.</summary>
    public IReadOnlyList<string> AddSources { get; init => field = value.ToArray(); } = [];
    /// <summary>Replacement NuGet package sources.</summary>
    public IReadOnlyList<string> Sources { get; init => field = value.ToArray(); } = [];
    /// <summary>An explicit target framework.</summary>
    public string? Framework { get; init; }
    /// <summary>Whether prerelease packages are eligible.</summary>
    public bool Prerelease { get; init; }
    /// <summary>Whether parallel restore is disabled.</summary>
    public bool DisableParallel { get; init; }
    /// <summary>Whether unavailable package sources are treated as warnings.</summary>
    public bool IgnoreFailedSources { get; init; }
    /// <summary>Whether NuGet caches are bypassed.</summary>
    public bool NoHttpCache { get; init; }
    /// <summary>Whether authentication may prompt interactively.</summary>
    public bool Interactive { get; init; }
    /// <summary>The restore logging verbosity.</summary>
    public string? Verbosity { get; init; }
    /// <summary>Whether an explicit downgrade is allowed.</summary>
    public bool AllowDowngrade { get; init; }
    /// <summary>Whether every tool in the selected manifest or scope is updated.</summary>
    public bool All { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "dotnet tool update",
            Arg(Package),
            Arg("--global", Global),
            Arg("--local", Local),
            Arg("--tool-path", ToolPath),
            Arg("--version", Version),
            Arg("--configfile", ConfigFile),
            Arg("--tool-manifest", ToolManifest),
            Args("--add-source", AddSources, " --add-source "),
            Args("--source", Sources, " --source "),
            Arg("--framework", Framework),
            Arg("--prerelease", Prerelease),
            Arg("--disable-parallel", DisableParallel),
            Arg("--ignore-failed-sources", IgnoreFailedSources),
            Arg("--no-http-cache", NoHttpCache),
            Arg("--interactive", Interactive),
            Arg("--verbosity", Verbosity),
            Arg("--allow-downgrade", AllowDowngrade),
            Arg("--all", All),
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

/// <summary>Watches project files and rebuilds, restarts, or hot-reloads the application after changes.</summary>
public sealed record DotNetWatch : ExecToolCommand
{
    /// <summary>Creates a command with output volume derived from the current logging level.</summary>
    public DotNetWatch()
    {
        (Quiet, Verbose) = DotNetOutputVolume.From(Logging.Level);
        Verbosity = MSBuildOutputVolume.From(Logging.Level).ToString().ToLowerInvariant();
        Configuration = MSBuildDefaults.Configuration;
    }
    /// <summary>Suppresses nonessential command output.</summary>
    public bool Quiet { get; init; }
    /// <summary>Writes diagnostic detail beyond normal command output.</summary>
    public bool Verbose { get; init; }
    /// <summary>Lists watched files without starting the application.</summary>
    public bool List { get; init; }
    /// <summary>Restarts the application for changes instead of applying Hot Reload edits.</summary>
    public bool NoHotReload { get; init; }
    /// <summary>Prevents the watcher from waiting for or requesting terminal input.</summary>
    public bool NonInteractive { get; init; }
    /// <summary>Selects the named build configuration.</summary>
    public string? Configuration { get; init; }
    /// <summary>Selects one target framework declared by the project.</summary>
    public string? Framework { get; init; }
    /// <summary>Targets the specified runtime identifier, such as <c>win-x64</c>.</summary>
    public string? Runtime { get; init; }
    /// <summary>Allows authentication and other operations to prompt for input.</summary>
    public bool Interactive { get; init; }
    /// <summary>Skips implicit restore; assets must already be current.</summary>
    public bool NoRestore { get; init; }
    /// <summary>Publishes and runs a self-contained application with the .NET runtime included.</summary>
    public bool? SelfContained { get; init; }
    /// <summary>Controls MSBuild output detail for watched builds.</summary>
    public string? Verbosity { get; init; }
    /// <summary>Shorthand target architecture combined with the default runtime identifier.</summary>
    public string? Architecture { get; init; }
    /// <summary>Shorthand target operating system combined with the default runtime identifier.</summary>
    public string? OperatingSystem { get; init; }
    /// <summary>Prevents reuse of persistent build servers during this invocation.</summary>
    public bool DisableBuildServers { get; init; }
    /// <summary>Places outputs for all projects beneath this artifacts root, separated by project.</summary>
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

/// <summary>Applies or verifies whitespace, style, and analyzer fixes across a project or solution.</summary>
public sealed record DotNetFormat : DotNetTargetCommand
{
    /// <summary>Selects which formatting category the command processes.</summary>
    public FormatCommand? Command { get; init; }
    /// <summary>Selects the workspace-loading operation used internally by the formatter.</summary>
    public string? CustomCommand { get; init; }
    /// <summary>Runs only the specified diagnostic IDs.</summary>
    public IReadOnlyList<string> Diagnostics { get; init => field = value.ToArray(); } = [];
    /// <summary>Excludes the specified diagnostic IDs from formatting.</summary>
    public IReadOnlyList<string> ExcludeDiagnostics { get; init => field = value.ToArray(); } = [];
    /// <summary>Applies diagnostics at or above the specified severity.</summary>
    public string? Severity { get; init; }
    /// <summary>Skips implicit restore; assets must already be current.</summary>
    public bool NoRestore { get; init; }
    /// <summary>Checks formatting and fails when files would change, without rewriting them.</summary>
    public bool VerifyNoChanges { get; init; }
    /// <summary>Formats only the specified files or folders, interpreted relative to the workspace.</summary>
    public IReadOnlyList<string> Include { get; init => field = value.ToArray(); } = [];
    /// <summary>Excludes the specified files or folders from formatting.</summary>
    public IReadOnlyList<string> Exclude { get; init => field = value.ToArray(); } = [];
    /// <summary>Includes generated-code files that formatting normally skips.</summary>
    public bool IncludeGenerated { get; init; }
    /// <summary>Writes an MSBuild binary log to this path.</summary>
    public string? BinaryLog { get; init; }
    /// <summary>Writes a JSON report describing files and diagnostics changed by formatting.</summary>
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
