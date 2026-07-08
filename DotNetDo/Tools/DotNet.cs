namespace DotNetDo;

public static partial class Tools
{
    public static class DotNet
    {
        public static readonly DotNetBuild Build = new();
        public static readonly DotNetClean Clean = new();
        public static readonly DotNetDevCerts DevCerts = new();
        public static readonly DotNetFormat Format = new();
        public static readonly DotNetPack Pack = new();
        public static readonly DotNetRestore Restore = new();
        public static readonly DotNetTest Test = new();
        public static readonly DotNetWatch Watch = new();
    }
}

public abstract record DotNetTargetCommand : ToolCommand
{
    public IReadOnlyList<string> Targets { get => GetArgumentArray("target"); init => SetArgumentArray("target", "", value); }
}

public abstract record DotNetBuildOptionsCommand : DotNetTargetCommand
{
    public bool CurrentRuntime { get => GetFlag("use-current-runtime"); init => SetFlag("use-current-runtime", "--use-current-runtime", value); }
    public string? Configuration { get => GetArgument("configuration"); init => SetArgument("configuration", "--configuration ", value); }
    public string? Runtime { get => GetArgument("runtime"); init => SetArgument("runtime", "--runtime ", value); }
    public string? VersionSuffix { get => GetArgument("version-suffix"); init => SetArgument("version-suffix", "--version-suffix ", value); }
    public bool NoRestore { get => GetFlag("no-restore"); init => SetFlag("no-restore", "--no-restore", value); }
    public bool Interactive { get => GetFlag("interactive"); init => SetFlag("interactive", "--interactive", value); }
    public string? Verbosity { get => GetArgument("verbosity"); init => SetArgument("verbosity", "--verbosity ", value); }
    public string? Output { get => GetArgument("output"); init => SetArgument("output", "--output ", value); }
    public string? ArtifactsPath { get => GetArgument("artifacts-path"); init => SetArgument("artifacts-path", "--artifacts-path ", value); }
    public bool NoLogo { get => GetFlag("nologo"); init => SetFlag("nologo", "--nologo", value); }
    public bool DisableBuildServers { get => GetFlag("disable-build-servers"); init => SetFlag("disable-build-servers", "--disable-build-servers", value); }
}

public sealed record DotNetBuild : DotNetBuildOptionsCommand
{
    protected override string CommandPrefix => "dotnet build";
    public string? Framework { get => GetArgument("framework"); init => SetArgument("framework", "--framework ", value); }
    public bool Debug { get => GetFlag("debug"); init => SetFlag("debug", "--debug", value); }
    public bool NoIncremental { get => GetFlag("no-incremental"); init => SetFlag("no-incremental", "--no-incremental", value); }
    public bool NoDependencies { get => GetFlag("no-dependencies"); init => SetFlag("no-dependencies", "--no-dependencies", value); }
    public bool? SelfContained { get => GetFlag("self-contained", "--self-contained", "--no-self-contained"); init => SetFlag("self-contained", "--self-contained", "--no-self-contained", value); }
    public string? Architecture { get => GetArgument("arch"); init => SetArgument("arch", "--arch ", value); }
    public string? OperatingSystem { get => GetArgument("os"); init => SetArgument("os", "--os ", value); }
}

public sealed record DotNetClean : DotNetTargetCommand
{
    protected override string CommandPrefix => "dotnet clean";
    public string? Framework { get => GetArgument("framework"); init => SetArgument("framework", "--framework ", value); }
    public string? Runtime { get => GetArgument("runtime"); init => SetArgument("runtime", "--runtime ", value); }
    public string? Configuration { get => GetArgument("configuration"); init => SetArgument("configuration", "--configuration ", value); }
    public bool Interactive { get => GetFlag("interactive"); init => SetFlag("interactive", "--interactive", value); }
    public string? Verbosity { get => GetArgument("verbosity"); init => SetArgument("verbosity", "--verbosity ", value); }
    public string? Output { get => GetArgument("output"); init => SetArgument("output", "--output ", value); }
    public string? ArtifactsPath { get => GetArgument("artifacts-path"); init => SetArgument("artifacts-path", "--artifacts-path ", value); }
    public bool NoLogo { get => GetFlag("nologo"); init => SetFlag("nologo", "--nologo", value); }
    public bool DisableBuildServers { get => GetFlag("disable-build-servers"); init => SetFlag("disable-build-servers", "--disable-build-servers", value); }
}

public sealed record DotNetDevCerts : ToolCommand
{
    protected override string CommandPrefix => "dotnet dev-certs https";
    public string? ExportPath { get => GetArgument("export-path"); init => SetArgument("export-path", "--export-path ", value); }
    public string? Password { get => GetArgument("password"); init => SetArgument("password", "--password ", value); }
    public bool NoPassword { get => GetFlag("no-password"); init => SetFlag("no-password", "--no-password", value); }
    public bool Check { get => GetFlag("check"); init => SetFlag("check", "--check", value); }
    public bool Clean { get => GetFlag("clean"); init => SetFlag("clean", "--clean", value); }
    public string? Import { get => GetArgument("import"); init => SetArgument("import", "--import ", value); }
    public string? Format { get => GetArgument("format"); init => SetArgument("format", "--format ", value); }
    public bool Trust { get => GetFlag("trust"); init => SetFlag("trust", "--trust", value); }
    public bool Verbose { get => GetFlag("verbose"); init => SetFlag("verbose", "--verbose", value); }
    public bool Quiet { get => GetFlag("quiet"); init => SetFlag("quiet", "--quiet", value); }
    public bool CheckTrustMachineReadable { get => GetFlag("check-trust-machine-readable"); init => SetFlag("check-trust-machine-readable", "--check-trust-machine-readable", value); }
}

public sealed record DotNetPack : DotNetBuildOptionsCommand
{
    protected override string CommandPrefix => "dotnet pack";
    public bool NoBuild { get => GetFlag("no-build"); init => SetFlag("no-build", "--no-build", value); }
    public bool IncludeSymbols { get => GetFlag("include-symbols"); init => SetFlag("include-symbols", "--include-symbols", value); }
    public bool IncludeSource { get => GetFlag("include-source"); init => SetFlag("include-source", "--include-source", value); }
    public bool Serviceable { get => GetFlag("serviceable"); init => SetFlag("serviceable", "--serviceable", value); }
    public string? Version { get => GetArgument("version"); init => SetArgument("version", "--version ", value); }
}

public sealed record DotNetRestore : DotNetTargetCommand
{
    protected override string CommandPrefix => "dotnet restore";
    public bool DisableBuildServers { get => GetFlag("disable-build-servers"); init => SetFlag("disable-build-servers", "--disable-build-servers", value); }
    public IReadOnlyList<string> Sources { get => GetArgumentArray("source", " --source "); init => SetArgumentArray("source", "--source ", value, " --source "); }
    public string? Packages { get => GetArgument("packages"); init => SetArgument("packages", "--packages ", value); }
    public bool CurrentRuntime { get => GetFlag("use-current-runtime"); init => SetFlag("use-current-runtime", "--use-current-runtime", value); }
    public bool DisableParallel { get => GetFlag("disable-parallel"); init => SetFlag("disable-parallel", "--disable-parallel", value); }
    public string? ConfigFile { get => GetArgument("configfile"); init => SetArgument("configfile", "--configfile ", value); }
    public bool NoHttpCache { get => GetFlag("no-http-cache"); init => SetFlag("no-http-cache", "--no-http-cache", value); }
    public bool IgnoreFailedSources { get => GetFlag("ignore-failed-sources"); init => SetFlag("ignore-failed-sources", "--ignore-failed-sources", value); }
    public bool Force { get => GetFlag("force"); init => SetFlag("force", "--force", value); }
    public string? Runtime { get => GetArgument("runtime"); init => SetArgument("runtime", "--runtime ", value); }
    public bool NoDependencies { get => GetFlag("no-dependencies"); init => SetFlag("no-dependencies", "--no-dependencies", value); }
    public string? Verbosity { get => GetArgument("verbosity"); init => SetArgument("verbosity", "--verbosity ", value); }
    public bool Interactive { get => GetFlag("interactive"); init => SetFlag("interactive", "--interactive", value); }
    public string? ArtifactsPath { get => GetArgument("artifacts-path"); init => SetArgument("artifacts-path", "--artifacts-path ", value); }
    public bool UseLockFile { get => GetFlag("use-lock-file"); init => SetFlag("use-lock-file", "--use-lock-file", value); }
    public bool LockedMode { get => GetFlag("locked-mode"); init => SetFlag("locked-mode", "--locked-mode", value); }
    public string? LockFilePath { get => GetArgument("lock-file-path"); init => SetArgument("lock-file-path", "--lock-file-path ", value); }
    public bool ForceEvaluate { get => GetFlag("force-evaluate"); init => SetFlag("force-evaluate", "--force-evaluate", value); }
    public string? Architecture { get => GetArgument("arch"); init => SetArgument("arch", "--arch ", value); }
    public string? OperatingSystem { get => GetArgument("os"); init => SetArgument("os", "--os ", value); }
}

public sealed record DotNetTest : DotNetTargetCommand
{
    protected override string CommandPrefix => "dotnet test";
    public string? Settings { get => GetArgument("settings"); init => SetArgument("settings", "--settings ", value); }
    public bool ListTests { get => GetFlag("list-tests"); init => SetFlag("list-tests", "--list-tests", value); }
    public IReadOnlyList<string> Environment { get => GetArgumentArray("environment", " --environment "); init => SetArgumentArray("environment", "--environment ", value, " --environment "); }
    public string? Filter { get => GetArgument("filter"); init => SetArgument("filter", "--filter ", value); }
    public string? TestAdapterPath { get => GetArgument("test-adapter-path"); init => SetArgument("test-adapter-path", "--test-adapter-path ", value); }
    public IReadOnlyList<string> Loggers { get => GetArgumentArray("logger", " --logger "); init => SetArgumentArray("logger", "--logger ", value, " --logger "); }
    public string? Output { get => GetArgument("output"); init => SetArgument("output", "--output ", value); }
    public string? ArtifactsPath { get => GetArgument("artifacts-path"); init => SetArgument("artifacts-path", "--artifacts-path ", value); }
    public string? Diag { get => GetArgument("diag"); init => SetArgument("diag", "--diag ", value); }
    public bool NoBuild { get => GetFlag("no-build"); init => SetFlag("no-build", "--no-build", value); }
    public string? ResultsDirectory { get => GetArgument("results-directory"); init => SetArgument("results-directory", "--results-directory ", value); }
    public string? Collect { get => GetArgument("collect"); init => SetArgument("collect", "--collect ", value); }
    public bool Blame { get => GetFlag("blame"); init => SetFlag("blame", "--blame", value); }
    public bool BlameCrash { get => GetFlag("blame-crash"); init => SetFlag("blame-crash", "--blame-crash", value); }
    public string? BlameCrashDumpType { get => GetArgument("blame-crash-dump-type"); init => SetArgument("blame-crash-dump-type", "--blame-crash-dump-type ", value); }
    public bool BlameCrashCollectAlways { get => GetFlag("blame-crash-collect-always"); init => SetFlag("blame-crash-collect-always", "--blame-crash-collect-always", value); }
    public bool BlameHang { get => GetFlag("blame-hang"); init => SetFlag("blame-hang", "--blame-hang", value); }
    public string? BlameHangDumpType { get => GetArgument("blame-hang-dump-type"); init => SetArgument("blame-hang-dump-type", "--blame-hang-dump-type ", value); }
    public string? BlameHangTimeout { get => GetArgument("blame-hang-timeout"); init => SetArgument("blame-hang-timeout", "--blame-hang-timeout ", value); }
    public bool NoLogo { get => GetFlag("nologo"); init => SetFlag("nologo", "--nologo", value); }
    public string? Configuration { get => GetArgument("configuration"); init => SetArgument("configuration", "--configuration ", value); }
    public string? Framework { get => GetArgument("framework"); init => SetArgument("framework", "--framework ", value); }
    public string? Runtime { get => GetArgument("runtime"); init => SetArgument("runtime", "--runtime ", value); }
    public bool NoRestore { get => GetFlag("no-restore"); init => SetFlag("no-restore", "--no-restore", value); }
    public bool Interactive { get => GetFlag("interactive"); init => SetFlag("interactive", "--interactive", value); }
    public string? Verbosity { get => GetArgument("verbosity"); init => SetArgument("verbosity", "--verbosity ", value); }
    public string? Architecture { get => GetArgument("arch"); init => SetArgument("arch", "--arch ", value); }
    public string? OperatingSystem { get => GetArgument("os"); init => SetArgument("os", "--os ", value); }
    public bool DisableBuildServers { get => GetFlag("disable-build-servers"); init => SetFlag("disable-build-servers", "--disable-build-servers", value); }
}

public sealed record DotNetWatch : ToolCommand
{
    protected override string CommandPrefix => "dotnet watch";
    public bool Quiet { get => GetFlag("quiet"); init => SetFlag("quiet", "--quiet", value); }
    public bool Verbose { get => GetFlag("verbose"); init => SetFlag("verbose", "--verbose", value); }
    public bool List { get => GetFlag("list"); init => SetFlag("list", "--list", value); }
    public bool NoHotReload { get => GetFlag("no-hot-reload"); init => SetFlag("no-hot-reload", "--no-hot-reload", value); }
    public bool NonInteractive { get => GetFlag("non-interactive"); init => SetFlag("non-interactive", "--non-interactive", value); }
    public string? Configuration { get => GetArgument("configuration"); init => SetArgument("configuration", "--configuration ", value); }
    public string? Framework { get => GetArgument("framework"); init => SetArgument("framework", "--framework ", value); }
    public string? Runtime { get => GetArgument("runtime"); init => SetArgument("runtime", "--runtime ", value); }
    public bool Interactive { get => GetFlag("interactive"); init => SetFlag("interactive", "--interactive", value); }
    public bool NoRestore { get => GetFlag("no-restore"); init => SetFlag("no-restore", "--no-restore", value); }
    public bool? SelfContained { get => GetFlag("self-contained", "--self-contained", "--no-self-contained"); init => SetFlag("self-contained", "--self-contained", "--no-self-contained", value); }
    public string? Verbosity { get => GetArgument("verbosity"); init => SetArgument("verbosity", "--verbosity ", value); }
    public string? Architecture { get => GetArgument("arch"); init => SetArgument("arch", "--arch ", value); }
    public string? OperatingSystem { get => GetArgument("os"); init => SetArgument("os", "--os ", value); }
    public bool DisableBuildServers { get => GetFlag("disable-build-servers"); init => SetFlag("disable-build-servers", "--disable-build-servers", value); }
    public string? ArtifactsPath { get => GetArgument("artifacts-path"); init => SetArgument("artifacts-path", "--artifacts-path ", value); }
}

public sealed record DotNetFormat : DotNetTargetCommand
{
    protected override string CommandPrefix => "dotnet format";
    public FormatCommand? Command { get => GetEnum<FormatCommand>("command"); init => SetEnum("command", value); }
    public string? CustomCommand { get => GetArgument("command"); init => SetArgument("command", value); }
    public IReadOnlyList<string> Diagnostics { get => GetArgumentArray("diagnostics"); init => SetArgumentArray("diagnostics", "--diagnostics ", value); }
    public IReadOnlyList<string> ExcludeDiagnostics { get => GetArgumentArray("exclude-diagnostics"); init => SetArgumentArray("exclude-diagnostics", "--exclude-diagnostics ", value); }
    public string? Severity { get => GetArgument("severity"); init => SetArgument("severity", "--severity ", value); }
    public bool NoRestore { get => GetFlag("no-restore"); init => SetFlag("no-restore", "--no-restore", value); }
    public bool VerifyNoChanges { get => GetFlag("verify-no-changes"); init => SetFlag("verify-no-changes", "--verify-no-changes", value); }
    public IReadOnlyList<string> Include { get => GetArgumentArray("include"); init => SetArgumentArray("include", "--include ", value); }
    public IReadOnlyList<string> Exclude { get => GetArgumentArray("exclude"); init => SetArgumentArray("exclude", "--exclude ", value); }
    public bool IncludeGenerated { get => GetFlag("include-generated"); init => SetFlag("include-generated", "--include-generated", value); }
    public string? Verbosity { get => GetArgument("verbosity"); init => SetArgument("verbosity", "--verbosity ", value); }
    public string? BinaryLog { get => GetArgument("binarylog"); init => SetArgument("binarylog", "--binarylog ", value); }
    public string? Report { get => GetArgument("report"); init => SetArgument("report", "--report ", value); }
}

public enum FormatCommand { Whitespace, Style, Analyzers }