namespace DotNetDo.Tools;

public static class DotNet
{
    public static ExecProcess Build(Action<DotNetBuild>? configure = null, ExecOptions? options = null) =>
        new DotNetBuild().Exec(configure, options);

    public static ExecProcess Clean(Action<DotNetClean>? configure = null, ExecOptions? options = null) =>
        new DotNetClean().Exec(configure, options);

    public static ExecProcess DevCerts(Action<DotNetDevCerts>? configure = null, ExecOptions? options = null) =>
        new DotNetDevCerts().Exec(configure, options);

    public static ExecProcess Format(Action<DotNetFormat>? configure = null, ExecOptions? options = null) =>
        new DotNetFormat().Exec(configure, options);

    public static ExecProcess Pack(Action<DotNetPack>? configure = null, ExecOptions? options = null) =>
        new DotNetPack().Exec(configure, options);

    public static ExecProcess Restore(Action<DotNetRestore>? configure = null, ExecOptions? options = null) => 
        new DotNetRestore().Exec(configure, options);

    public static ExecProcess Test(Action<DotNetTest>? configure = null, ExecOptions? options = null) =>
        new DotNetTest().Exec(configure, options);

    public static ExecProcess Watch(Action<DotNetWatch>? configure = null, ExecOptions? options = null) =>
        new DotNetWatch().Exec(configure, options);
}

public abstract class DotNetTargetCommand<TSelf> : ToolConfig<TSelf> where TSelf : DotNetTargetCommand<TSelf>
{
    protected DotNetTargetCommand()
    {
        WithArgument("target", "");
    }

    public TSelf WithTarget(string target) => WithArgument("target", target);

    public TSelf WithTargets(params string[] targets) => WithArgument("target", string.Join(' ', targets));

    public TSelf WithoutTarget() => WithoutArgument("target");
}

public abstract class DotNetBuildOptions<TSelf> : DotNetTargetCommand<TSelf>
    where TSelf : DotNetBuildOptions<TSelf>
{
    public TSelf WithCurrentRuntime() => WithArgument("use-current-runtime", "--use-current-runtime");
    public TSelf WithoutCurrentRuntime() => WithoutArgument("use-current-runtime");
    public TSelf WithConfiguration(string configuration) => WithArgument("configuration", $"--configuration {configuration}");
    public TSelf WithoutConfiguration() => WithoutArgument("configuration");
    public TSelf WithRuntime(string runtimeIdentifier) => WithArgument("runtime", $"--runtime {runtimeIdentifier}");
    public TSelf WithoutRuntime() => WithoutArgument("runtime");
    public TSelf WithVersionSuffix(string versionSuffix) => WithArgument("version-suffix", $"--version-suffix {versionSuffix}");
    public TSelf WithoutVersionSuffix() => WithoutArgument("version-suffix");
    public TSelf WithNoRestore() => WithArgument("no-restore", "--no-restore");
    public TSelf WithoutNoRestore() => WithoutArgument("no-restore");
    public TSelf WithInteractive() => WithArgument("interactive", "--interactive");
    public TSelf WithoutInteractive() => WithoutArgument("interactive");
    public TSelf WithVerbosity(string level) => WithArgument("verbosity", $"--verbosity {level}");
    public TSelf WithoutVerbosity() => WithoutArgument("verbosity");
    public TSelf WithOutput(string outputDirectory) => WithArgument("output", $"--output {outputDirectory}");
    public TSelf WithoutOutput() => WithoutArgument("output");
    public TSelf WithArtifactsPath(string artifactsDirectory) => WithArgument("artifacts-path", $"--artifacts-path {artifactsDirectory}");
    public TSelf WithoutArtifactsPath() => WithoutArgument("artifacts-path");
    public TSelf WithNoLogo() => WithArgument("nologo", "--nologo");
    public TSelf WithoutNoLogo() => WithoutArgument("nologo");
    public TSelf WithDisableBuildServers() => WithArgument("disable-build-servers", "--disable-build-servers");
    public TSelf WithoutDisableBuildServers() => WithoutArgument("disable-build-servers");
}

public sealed class DotNetBuild : DotNetBuildOptions<DotNetBuild>
{
    internal DotNetBuild() { }

    protected override string CommandPrefix => "dotnet build";

    public DotNetBuild WithFramework(string framework) => WithArgument("framework", $"--framework {framework}");
    public DotNetBuild WithoutFramework() => WithoutArgument("framework");
    public DotNetBuild WithDebug() => WithArgument("debug", "--debug");
    public DotNetBuild WithoutDebug() => WithoutArgument("debug");
    public DotNetBuild WithNoIncremental() => WithArgument("no-incremental", "--no-incremental");
    public DotNetBuild WithoutNoIncremental() => WithoutArgument("no-incremental");
    public DotNetBuild WithNoDependencies() => WithArgument("no-dependencies", "--no-dependencies");
    public DotNetBuild WithoutNoDependencies() => WithoutArgument("no-dependencies");
    public DotNetBuild WithSelfContained() => WithArgument("self-contained", "--self-contained");
    public DotNetBuild WithNoSelfContained() => WithArgument("self-contained", "--no-self-contained");
    public DotNetBuild WithoutSelfContained() => WithoutArgument("self-contained");
    public DotNetBuild WithArchitecture(string architecture) => WithArgument("arch", $"--arch {architecture}");
    public DotNetBuild WithoutArchitecture() => WithoutArgument("arch");
    public DotNetBuild WithOperatingSystem(string operatingSystem) => WithArgument("os", $"--os {operatingSystem}");
    public DotNetBuild WithoutOperatingSystem() => WithoutArgument("os");
}

public sealed class DotNetClean : DotNetTargetCommand<DotNetClean>
{
    internal DotNetClean() { }

    protected override string CommandPrefix => "dotnet clean";

    public DotNetClean WithFramework(string framework) => WithArgument("framework", $"--framework {framework}");
    public DotNetClean WithoutFramework() => WithoutArgument("framework");
    public DotNetClean WithRuntime(string runtimeIdentifier) => WithArgument("runtime", $"--runtime {runtimeIdentifier}");
    public DotNetClean WithoutRuntime() => WithoutArgument("runtime");
    public DotNetClean WithConfiguration(string configuration) => WithArgument("configuration", $"--configuration {configuration}");
    public DotNetClean WithoutConfiguration() => WithoutArgument("configuration");
    public DotNetClean WithInteractive() => WithArgument("interactive", "--interactive");
    public DotNetClean WithoutInteractive() => WithoutArgument("interactive");
    public DotNetClean WithVerbosity(string level) => WithArgument("verbosity", $"--verbosity {level}");
    public DotNetClean WithoutVerbosity() => WithoutArgument("verbosity");
    public DotNetClean WithOutput(string outputDirectory) => WithArgument("output", $"--output {outputDirectory}");
    public DotNetClean WithoutOutput() => WithoutArgument("output");
    public DotNetClean WithArtifactsPath(string artifactsDirectory) => WithArgument("artifacts-path", $"--artifacts-path {artifactsDirectory}");
    public DotNetClean WithoutArtifactsPath() => WithoutArgument("artifacts-path");
    public DotNetClean WithNoLogo() => WithArgument("nologo", "--nologo");
    public DotNetClean WithoutNoLogo() => WithoutArgument("nologo");
    public DotNetClean WithDisableBuildServers() => WithArgument("disable-build-servers", "--disable-build-servers");
    public DotNetClean WithoutDisableBuildServers() => WithoutArgument("disable-build-servers");
}

public sealed class DotNetDevCerts : ToolConfig<DotNetDevCerts>
{
    internal DotNetDevCerts() { }

    protected override string CommandPrefix => "dotnet dev-certs https";

    public DotNetDevCerts WithExportPath(string path) => WithArgument("export-path", $"--export-path {path}");
    public DotNetDevCerts WithoutExportPath() => WithoutArgument("export-path");
    public DotNetDevCerts WithPassword(string password) => WithArgument("password", $"--password {password}");
    public DotNetDevCerts WithoutPassword() => WithoutArgument("password");
    public DotNetDevCerts WithNoPassword() => WithArgument("no-password", "--no-password");
    public DotNetDevCerts WithoutNoPassword() => WithoutArgument("no-password");
    public DotNetDevCerts WithCheck() => WithArgument("check", "--check");
    public DotNetDevCerts WithoutCheck() => WithoutArgument("check");
    public DotNetDevCerts WithClean() => WithArgument("clean", "--clean");
    public DotNetDevCerts WithoutClean() => WithoutArgument("clean");
    public DotNetDevCerts WithImport(string path) => WithArgument("import", $"--import {path}");
    public DotNetDevCerts WithoutImport() => WithoutArgument("import");
    public DotNetDevCerts WithFormat(string format) => WithArgument("format", $"--format {format}");
    public DotNetDevCerts WithoutFormat() => WithoutArgument("format");
    public DotNetDevCerts WithTrust() => WithArgument("trust", "--trust");
    public DotNetDevCerts WithoutTrust() => WithoutArgument("trust");
    public DotNetDevCerts WithVerbose() => WithArgument("verbose", "--verbose");
    public DotNetDevCerts WithoutVerbose() => WithoutArgument("verbose");
    public DotNetDevCerts WithQuiet() => WithArgument("quiet", "--quiet");
    public DotNetDevCerts WithoutQuiet() => WithoutArgument("quiet");
    public DotNetDevCerts WithCheckTrustMachineReadable() => WithArgument("check-trust-machine-readable", "--check-trust-machine-readable");
    public DotNetDevCerts WithoutCheckTrustMachineReadable() => WithoutArgument("check-trust-machine-readable");
}

public sealed class DotNetFormat : DotNetTargetCommand<DotNetFormat>
{
    internal DotNetFormat()
    {
        WithArgument("command", "");
    }

    protected override string CommandPrefix => "dotnet format";

    public DotNetFormat WithWhitespace() => WithArgument("command", "whitespace");
    public DotNetFormat WithStyle() => WithArgument("command", "style");
    public DotNetFormat WithAnalyzers() => WithArgument("command", "analyzers");
    public DotNetFormat WithoutCommand() => WithoutArgument("command");
    public DotNetFormat WithDiagnostics(params string[] diagnostics) => WithArgument("diagnostics", $"--diagnostics {string.Join(' ', diagnostics)}");
    public DotNetFormat WithoutDiagnostics() => WithoutArgument("diagnostics");
    public DotNetFormat WithExcludeDiagnostics(params string[] diagnostics) => WithArgument("exclude-diagnostics", $"--exclude-diagnostics {string.Join(' ', diagnostics)}");
    public DotNetFormat WithoutExcludeDiagnostics() => WithoutArgument("exclude-diagnostics");
    public DotNetFormat WithSeverity(string severity) => WithArgument("severity", $"--severity {severity}");
    public DotNetFormat WithoutSeverity() => WithoutArgument("severity");
    public DotNetFormat WithNoRestore() => WithArgument("no-restore", "--no-restore");
    public DotNetFormat WithoutNoRestore() => WithoutArgument("no-restore");
    public DotNetFormat WithVerifyNoChanges() => WithArgument("verify-no-changes", "--verify-no-changes");
    public DotNetFormat WithoutVerifyNoChanges() => WithoutArgument("verify-no-changes");
    public DotNetFormat WithInclude(params string[] paths) => WithArgument("include", $"--include {string.Join(' ', paths)}");
    public DotNetFormat WithoutInclude() => WithoutArgument("include");
    public DotNetFormat WithExclude(params string[] paths) => WithArgument("exclude", $"--exclude {string.Join(' ', paths)}");
    public DotNetFormat WithoutExclude() => WithoutArgument("exclude");
    public DotNetFormat WithIncludeGenerated() => WithArgument("include-generated", "--include-generated");
    public DotNetFormat WithoutIncludeGenerated() => WithoutArgument("include-generated");
    public DotNetFormat WithVerbosity(string level) => WithArgument("verbosity", $"--verbosity {level}");
    public DotNetFormat WithoutVerbosity() => WithoutArgument("verbosity");
    public DotNetFormat WithBinaryLog(string path) => WithArgument("binarylog", $"--binarylog {path}");
    public DotNetFormat WithoutBinaryLog() => WithoutArgument("binarylog");
    public DotNetFormat WithReport(string path) => WithArgument("report", $"--report {path}");
    public DotNetFormat WithoutReport() => WithoutArgument("report");
}

public sealed class DotNetPack : DotNetBuildOptions<DotNetPack>
{
    internal DotNetPack() { }

    protected override string CommandPrefix => "dotnet pack";

    public DotNetPack WithNoBuild() => WithArgument("no-build", "--no-build");
    public DotNetPack WithoutNoBuild() => WithoutArgument("no-build");
    public DotNetPack WithIncludeSymbols() => WithArgument("include-symbols", "--include-symbols");
    public DotNetPack WithoutIncludeSymbols() => WithoutArgument("include-symbols");
    public DotNetPack WithIncludeSource() => WithArgument("include-source", "--include-source");
    public DotNetPack WithoutIncludeSource() => WithoutArgument("include-source");
    public DotNetPack WithServiceable() => WithArgument("serviceable", "--serviceable");
    public DotNetPack WithoutServiceable() => WithoutArgument("serviceable");
    public DotNetPack WithVersion(string version) => WithArgument("version", $"--version {version}");
    public DotNetPack WithoutVersion() => WithoutArgument("version");
}

public sealed class DotNetRestore : DotNetTargetCommand<DotNetRestore>
{
    internal DotNetRestore() { }
    
    protected override string CommandPrefix => "dotnet restore";

    public DotNetRestore WithDisableBuildServers() => WithArgument("disable-build-servers", "--disable-build-servers");
    public DotNetRestore WithoutDisableBuildServers() => WithoutArgument("disable-build-servers");
    public DotNetRestore WithSource(string source) => WithArgument("source", $"--source {source}");
    public DotNetRestore WithSources(params string[] sources) => WithArgument("source", string.Join(' ', sources.Select(source => $"--source {source}")));
    public DotNetRestore WithoutSource() => WithoutArgument("source");
    public DotNetRestore WithPackages(string packagesDirectory) => WithArgument("packages", $"--packages {packagesDirectory}");
    public DotNetRestore WithoutPackages() => WithoutArgument("packages");
    public DotNetRestore WithCurrentRuntime() => WithArgument("use-current-runtime", "--use-current-runtime");
    public DotNetRestore WithoutCurrentRuntime() => WithoutArgument("use-current-runtime");
    public DotNetRestore WithDisableParallel() => WithArgument("disable-parallel", "--disable-parallel");
    public DotNetRestore WithoutDisableParallel() => WithoutArgument("disable-parallel");
    public DotNetRestore WithConfigFile(string file) => WithArgument("configfile", $"--configfile {file}");
    public DotNetRestore WithoutConfigFile() => WithoutArgument("configfile");
    public DotNetRestore WithNoHttpCache() => WithArgument("no-http-cache", "--no-http-cache");
    public DotNetRestore WithoutNoHttpCache() => WithoutArgument("no-http-cache");
    public DotNetRestore WithIgnoreFailedSources() => WithArgument("ignore-failed-sources", "--ignore-failed-sources");
    public DotNetRestore WithoutIgnoreFailedSources() => WithoutArgument("ignore-failed-sources");
    public DotNetRestore WithForce() => WithArgument("force", "--force");
    public DotNetRestore WithoutForce() => WithoutArgument("force");
    public DotNetRestore WithRuntime(string runtimeIdentifier) => WithArgument("runtime", $"--runtime {runtimeIdentifier}");
    public DotNetRestore WithoutRuntime() => WithoutArgument("runtime");
    public DotNetRestore WithNoDependencies() => WithArgument("no-dependencies", "--no-dependencies");
    public DotNetRestore WithoutNoDependencies() => WithoutArgument("no-dependencies");
    public DotNetRestore WithVerbosity(string level) => WithArgument("verbosity", $"--verbosity {level}");
    public DotNetRestore WithoutVerbosity() => WithoutArgument("verbosity");
    public DotNetRestore WithInteractive() => WithArgument("interactive", "--interactive");
    public DotNetRestore WithoutInteractive() => WithoutArgument("interactive");
    public DotNetRestore WithArtifactsPath(string artifactsDirectory) => WithArgument("artifacts-path", $"--artifacts-path {artifactsDirectory}");
    public DotNetRestore WithoutArtifactsPath() => WithoutArgument("artifacts-path");
    public DotNetRestore WithUseLockFile() => WithArgument("use-lock-file", "--use-lock-file");
    public DotNetRestore WithoutUseLockFile() => WithoutArgument("use-lock-file");
    public DotNetRestore WithLockedMode() => WithArgument("locked-mode", "--locked-mode");
    public DotNetRestore WithoutLockedMode() => WithoutArgument("locked-mode");
    public DotNetRestore WithLockFilePath(string lockFilePath) => WithArgument("lock-file-path", $"--lock-file-path {lockFilePath}");
    public DotNetRestore WithoutLockFilePath() => WithoutArgument("lock-file-path");
    public DotNetRestore WithForceEvaluate() => WithArgument("force-evaluate", "--force-evaluate");
    public DotNetRestore WithoutForceEvaluate() => WithoutArgument("force-evaluate");
    public DotNetRestore WithArchitecture(string architecture) => WithArgument("arch", $"--arch {architecture}");
    public DotNetRestore WithoutArchitecture() => WithoutArgument("arch");
    public DotNetRestore WithOperatingSystem(string operatingSystem) => WithArgument("os", $"--os {operatingSystem}");
    public DotNetRestore WithoutOperatingSystem() => WithoutArgument("os");
}

public sealed class DotNetTest : DotNetTargetCommand<DotNetTest>
{
    internal DotNetTest() { }

    protected override string CommandPrefix => "dotnet test";

    public DotNetTest WithSettings(string settingsFile) => WithArgument("settings", $"--settings {settingsFile}");
    public DotNetTest WithoutSettings() => WithoutArgument("settings");
    public DotNetTest WithListTests() => WithArgument("list-tests", "--list-tests");
    public DotNetTest WithoutListTests() => WithoutArgument("list-tests");
    public DotNetTest WithEnvironment(string name, string value) => WithArgument($"environment:{name}", $"--environment {name}={value}");
    public DotNetTest WithEnvironments(params string[] variables) => WithArgument("environment", string.Join(' ', variables.Select(variable => $"--environment {variable}")));
    public DotNetTest WithoutEnvironment(string name) => WithoutArgument($"environment:{name}");
    public DotNetTest WithoutEnvironments() => WithoutArgument("environment");
    public DotNetTest WithFilter(string expression) => WithArgument("filter", $"--filter {expression}");
    public DotNetTest WithoutFilter() => WithoutArgument("filter");
    public DotNetTest WithTestAdapterPath(string path) => WithArgument("test-adapter-path", $"--test-adapter-path {path}");
    public DotNetTest WithoutTestAdapterPath() => WithoutArgument("test-adapter-path");
    public DotNetTest WithLogger(string logger) => WithArgument("logger", $"--logger {logger}");
    public DotNetTest WithLoggers(params string[] loggers) => WithArgument("logger", string.Join(' ', loggers.Select(logger => $"--logger {logger}")));
    public DotNetTest WithoutLogger() => WithoutArgument("logger");
    public DotNetTest WithOutput(string outputDirectory) => WithArgument("output", $"--output {outputDirectory}");
    public DotNetTest WithoutOutput() => WithoutArgument("output");
    public DotNetTest WithArtifactsPath(string artifactsDirectory) => WithArgument("artifacts-path", $"--artifacts-path {artifactsDirectory}");
    public DotNetTest WithoutArtifactsPath() => WithoutArgument("artifacts-path");
    public DotNetTest WithDiag(string logFile) => WithArgument("diag", $"--diag {logFile}");
    public DotNetTest WithoutDiag() => WithoutArgument("diag");
    public DotNetTest WithNoBuild() => WithArgument("no-build", "--no-build");
    public DotNetTest WithoutNoBuild() => WithoutArgument("no-build");
    public DotNetTest WithResultsDirectory(string directory) => WithArgument("results-directory", $"--results-directory {directory}");
    public DotNetTest WithoutResultsDirectory() => WithoutArgument("results-directory");
    public DotNetTest WithCollect(string dataCollectorName) => WithArgument("collect", $"--collect {dataCollectorName}");
    public DotNetTest WithoutCollect() => WithoutArgument("collect");
    public DotNetTest WithBlame() => WithArgument("blame", "--blame");
    public DotNetTest WithoutBlame() => WithoutArgument("blame");
    public DotNetTest WithBlameCrash() => WithArgument("blame-crash", "--blame-crash");
    public DotNetTest WithoutBlameCrash() => WithoutArgument("blame-crash");
    public DotNetTest WithBlameCrashDumpType(string dumpType) => WithArgument("blame-crash-dump-type", $"--blame-crash-dump-type {dumpType}");
    public DotNetTest WithoutBlameCrashDumpType() => WithoutArgument("blame-crash-dump-type");
    public DotNetTest WithBlameCrashCollectAlways() => WithArgument("blame-crash-collect-always", "--blame-crash-collect-always");
    public DotNetTest WithoutBlameCrashCollectAlways() => WithoutArgument("blame-crash-collect-always");
    public DotNetTest WithBlameHang() => WithArgument("blame-hang", "--blame-hang");
    public DotNetTest WithoutBlameHang() => WithoutArgument("blame-hang");
    public DotNetTest WithBlameHangDumpType(string dumpType) => WithArgument("blame-hang-dump-type", $"--blame-hang-dump-type {dumpType}");
    public DotNetTest WithoutBlameHangDumpType() => WithoutArgument("blame-hang-dump-type");
    public DotNetTest WithBlameHangTimeout(string timeout) => WithArgument("blame-hang-timeout", $"--blame-hang-timeout {timeout}");
    public DotNetTest WithoutBlameHangTimeout() => WithoutArgument("blame-hang-timeout");
    public DotNetTest WithNoLogo() => WithArgument("nologo", "--nologo");
    public DotNetTest WithoutNoLogo() => WithoutArgument("nologo");
    public DotNetTest WithConfiguration(string configuration) => WithArgument("configuration", $"--configuration {configuration}");
    public DotNetTest WithoutConfiguration() => WithoutArgument("configuration");
    public DotNetTest WithFramework(string framework) => WithArgument("framework", $"--framework {framework}");
    public DotNetTest WithoutFramework() => WithoutArgument("framework");
    public DotNetTest WithRuntime(string runtimeIdentifier) => WithArgument("runtime", $"--runtime {runtimeIdentifier}");
    public DotNetTest WithoutRuntime() => WithoutArgument("runtime");
    public DotNetTest WithNoRestore() => WithArgument("no-restore", "--no-restore");
    public DotNetTest WithoutNoRestore() => WithoutArgument("no-restore");
    public DotNetTest WithInteractive() => WithArgument("interactive", "--interactive");
    public DotNetTest WithoutInteractive() => WithoutArgument("interactive");
    public DotNetTest WithVerbosity(string level) => WithArgument("verbosity", $"--verbosity {level}");
    public DotNetTest WithoutVerbosity() => WithoutArgument("verbosity");
    public DotNetTest WithArchitecture(string architecture) => WithArgument("arch", $"--arch {architecture}");
    public DotNetTest WithoutArchitecture() => WithoutArgument("arch");
    public DotNetTest WithOperatingSystem(string operatingSystem) => WithArgument("os", $"--os {operatingSystem}");
    public DotNetTest WithoutOperatingSystem() => WithoutArgument("os");
    public DotNetTest WithDisableBuildServers() => WithArgument("disable-build-servers", "--disable-build-servers");
    public DotNetTest WithoutDisableBuildServers() => WithoutArgument("disable-build-servers");
}

public sealed class DotNetWatch : ToolConfig<DotNetWatch>
{
    internal DotNetWatch() { }

    protected override string CommandPrefix => "dotnet watch";

    public DotNetWatch WithQuiet() => WithArgument("quiet", "--quiet");
    public DotNetWatch WithoutQuiet() => WithoutArgument("quiet");
    public DotNetWatch WithVerbose() => WithArgument("verbose", "--verbose");
    public DotNetWatch WithoutVerbose() => WithoutArgument("verbose");
    public DotNetWatch WithList() => WithArgument("list", "--list");
    public DotNetWatch WithoutList() => WithoutArgument("list");
    public DotNetWatch WithNoHotReload() => WithArgument("no-hot-reload", "--no-hot-reload");
    public DotNetWatch WithoutNoHotReload() => WithoutArgument("no-hot-reload");
    public DotNetWatch WithNonInteractive() => WithArgument("non-interactive", "--non-interactive");
    public DotNetWatch WithoutNonInteractive() => WithoutArgument("non-interactive");
    public DotNetWatch WithConfiguration(string configuration) => WithArgument("configuration", $"--configuration {configuration}");
    public DotNetWatch WithoutConfiguration() => WithoutArgument("configuration");
    public DotNetWatch WithFramework(string framework) => WithArgument("framework", $"--framework {framework}");
    public DotNetWatch WithoutFramework() => WithoutArgument("framework");
    public DotNetWatch WithRuntime(string runtimeIdentifier) => WithArgument("runtime", $"--runtime {runtimeIdentifier}");
    public DotNetWatch WithoutRuntime() => WithoutArgument("runtime");
    public DotNetWatch WithInteractive() => WithArgument("interactive", "--interactive");
    public DotNetWatch WithoutInteractive() => WithoutArgument("interactive");
    public DotNetWatch WithNoRestore() => WithArgument("no-restore", "--no-restore");
    public DotNetWatch WithoutNoRestore() => WithoutArgument("no-restore");
    public DotNetWatch WithSelfContained() => WithArgument("self-contained", "--self-contained");
    public DotNetWatch WithNoSelfContained() => WithArgument("self-contained", "--no-self-contained");
    public DotNetWatch WithoutSelfContained() => WithoutArgument("self-contained");
    public DotNetWatch WithVerbosity(string level) => WithArgument("verbosity", $"--verbosity {level}");
    public DotNetWatch WithoutVerbosity() => WithoutArgument("verbosity");
    public DotNetWatch WithArchitecture(string architecture) => WithArgument("arch", $"--arch {architecture}");
    public DotNetWatch WithoutArchitecture() => WithoutArgument("arch");
    public DotNetWatch WithOperatingSystem(string operatingSystem) => WithArgument("os", $"--os {operatingSystem}");
    public DotNetWatch WithoutOperatingSystem() => WithoutArgument("os");
    public DotNetWatch WithDisableBuildServers() => WithArgument("disable-build-servers", "--disable-build-servers");
    public DotNetWatch WithoutDisableBuildServers() => WithoutArgument("disable-build-servers");
    public DotNetWatch WithArtifactsPath(string artifactsDirectory) => WithArgument("artifacts-path", $"--artifacts-path {artifactsDirectory}");
    public DotNetWatch WithoutArtifactsPath() => WithoutArgument("artifacts-path");
}
