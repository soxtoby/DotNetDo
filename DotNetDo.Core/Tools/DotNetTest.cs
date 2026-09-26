using System.Text.Json;

namespace DotNetDo;

/// <summary>Builds selected test projects and runs their tests with the repository's test runner mode.</summary>
/// <remarks>
/// The repository selects VSTest or Microsoft.Testing.Platform for <c>dotnet test</c> through <c>global.json</c> or
/// <c>DOTNET_TEST_RUNNER</c>. The command detects that mode from its working directory, renders shared options for it, and
/// fails before execution when <see cref="VSTest"/> or <see cref="TestingPlatform"/> options don't match it.
/// <para>
/// Test report files, such as TRX or JUnit XML, aren't published to the CI server by this command. In Azure Pipelines,
/// add a <c>PublishTestResults@2</c> step after the task, with <c>condition: succeededOrFailed()</c> so that results
/// from failing runs are published too. Set <c>testResultsFormat</c> to <c>VSTest</c> for TRX files or <c>JUnit</c> for
/// JUnit XML. With Microsoft.Testing.Platform,
/// <see cref="DotNetTestingPlatformConfig.PublishAzureDevOpsTestResults"/> can publish results directly instead.
/// </para>
/// </remarks>
public sealed record DotNetTest : DotNetTargetCommand
{
    /// <summary>Creates a command with defaults for the current build locality.</summary>
    public DotNetTest() => Configuration = MSBuildDefaults.Configuration;

    /// <summary>Options that apply only when the repository runs <c>dotnet test</c> with VSTest.</summary>
    public DotNetVSTestConfig? VSTest { get; init; }
    /// <summary>Options that apply only when the repository runs <c>dotnet test</c> with Microsoft.Testing.Platform.</summary>
    public DotNetTestingPlatformConfig? TestingPlatform { get; init; }

    /// <summary>Discovers and lists tests without executing them.</summary>
    public bool ListTests { get; init; }
    /// <summary>Sets test-host environment variables as <c>NAME=VALUE</c>.</summary>
    public IReadOnlyList<string> TestEnvironment { get; init => field = value.ToArray(); } = [];
    /// <summary>Places outputs for all projects beneath this artifacts root, separated by project.</summary>
    public string? ArtifactsPath { get; init; }
    /// <summary>Skips building before the operation; required outputs must already exist.</summary>
    public bool NoBuild { get; init; }
    /// <summary>Places test results and generated artifacts in this directory.</summary>
    public string? ResultsDirectory { get; init; }
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
    /// <summary>Shorthand target architecture combined with the default runtime identifier.</summary>
    public string? Architecture { get; init; }
    /// <summary>Shorthand target operating system combined with the default runtime identifier.</summary>
    public string? OperatingSystem { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        DotNetTestRunner.UsesTestingPlatform(WorkingDirectory ?? Do.WorkingDirectory)
            ? TestingPlatformParts()
            : VSTestParts();

    IReadOnlyList<string?> VSTestParts()
    {
        if (TestingPlatform is not null)
            throw new InvalidOperationException(
                $"This repository runs dotnet test with VSTest, so {nameof(TestingPlatform)} options can't be used. " +
                $"Opt into Microsoft.Testing.Platform in global.json, or use {nameof(VSTest)} options instead.");

        var vstest = VSTest ?? new();
        return
            [
                "dotnet test",
                ..TargetParts,
                ..SharedParts(Arg("--list-tests", ListTests)),
                Arg("--settings", vstest.Settings),
                Arg("--filter", vstest.Filter),
                Arg("--test-adapter-path", vstest.TestAdapterPath),
                Args("--logger", vstest.Loggers, " --logger "),
                Arg("--output", vstest.Output),
                Arg("--diag", vstest.Diag),
                Arg("--collect", vstest.Collect),
                Arg("--blame", vstest.Blame),
                Arg("--blame-crash", vstest.BlameCrash),
                Arg("--blame-crash-dump-type", vstest.BlameCrashDumpType),
                Arg("--blame-crash-collect-always", vstest.BlameCrashCollectAlways),
                Arg("--blame-hang", vstest.BlameHang),
                Arg("--blame-hang-dump-type", vstest.BlameHangDumpType),
                Arg("--blame-hang-timeout", vstest.BlameHangTimeout),
                Arg("--interactive", vstest.Interactive),
                Arg("--disable-build-servers", vstest.DisableBuildServers),
            ];
    }

    IReadOnlyList<string?> TestingPlatformParts()
    {
        if (VSTest is not null)
            throw new InvalidOperationException(
                $"This repository runs dotnet test with Microsoft.Testing.Platform, so {nameof(VSTest)} options can't be used. " +
                $"Use {nameof(TestingPlatform)} options instead.");

        var platform = TestingPlatform ?? new();
        var listTests = ListTests || platform.ListTestsFormat is not null
            ? $"--list-tests{(platform.ListTestsFormat is { } format ? " " + format.ToString().ToLowerInvariant() : "")}"
            : null;
        var applicationParts = TestApplicationParts(platform);

        return
            [
                "dotnet test",
                .. TestingPlatformSelection(platform),
                Arg("--verbosity", Verbosity),
                .. SharedParts(listTests),
                Arg("--max-parallel-test-modules", platform.MaxParallelTestModules),
                Arg("--config-file", platform.ConfigFile),
                Arg("--results-directory-layout", platform.ResultsDirectoryLayout?.ToString().ToKebabCaseLower()),
                Arg("--diagnostic-output-directory", platform.DiagnosticOutputDirectory),
                Arg("--minimum-expected-tests", platform.MinimumExpectedTests),
                Arg("--maximum-failed-tests", platform.MaximumFailedTests),
                Arg("--timeout", platform.Timeout),
                Arg("--use-current-runtime", platform.CurrentRuntime),
                Arg("--no-dependencies", platform.NoDependencies),
                Arg("--no-ansi", platform.NoAnsi),
                Arg("--no-progress", platform.NoProgress),
                Arg("--no-artifact-post-processing", platform.NoArtifactPostProcessing),
                Arg("--output", platform.Output?.ToString()),
                Arg("--show-test-results", RenderOutcomes(platform.ShowTestResults)),
                Arg("--no-launch-profile", platform.NoLaunchProfile),
                Arg("--no-launch-profile-arguments", platform.NoLaunchProfileArguments),
                Arg("--device", platform.Device),
                Arg("--list-devices", platform.ListDevices),
                Arg("--collect-test-map", platform.CollectTestMap),
                Arg("--affected-tests", platform.AffectedTests),
                applicationParts.Any(part => !string.IsNullOrWhiteSpace(part)) ? "--" : null,
                .. applicationParts,
            ];
    }

    IReadOnlyList<string?> SharedParts(string? listTests) =>
        [
            listTests,
            Args("--environment", TestEnvironment, " --environment "),
            Arg("--artifacts-path", ArtifactsPath),
            Arg("--no-build", NoBuild),
            Arg("--results-directory", ResultsDirectory),
            Arg("--nologo", NoLogo),
            Arg("--configuration", Configuration),
            Arg("--framework", Framework),
            Arg("--runtime", Runtime),
            Arg("--no-restore", NoRestore),
            Arg("--arch", Architecture),
            Arg("--os", OperatingSystem),
        ];

    IReadOnlyList<string?> TestingPlatformSelection(DotNetTestingPlatformConfig platform)
    {
        if (platform.TestModules is not null)
            return [Arg("--test-modules", platform.TestModules), Arg("--root-directory", platform.RootDirectory)];
        if (Targets.Count > 1)
            throw new InvalidOperationException(
                $"Microsoft.Testing.Platform runs one solution or project at a time; use {nameof(DotNetTestingPlatformConfig.TestModules)} to select several test modules.");
        if (Targets.Count == 0)
            return [Arg("--root-directory", platform.RootDirectory)];

        var target = Targets[0];
        var extension = Path.GetExtension(target);
        var option = extension.ToLowerInvariant() switch
            {
                ".sln" or ".slnx" or ".slnf" => "--solution",
                _ when extension.EndsWith("proj", StringComparison.OrdinalIgnoreCase) => "--project",
                _ => throw new InvalidOperationException(
                    $"Microsoft.Testing.Platform target '{target}' must be a solution or project file; use {nameof(DotNetTestingPlatformConfig.TestModules)} to select built test modules."),
            };
        return [Arg(option, target), Arg("--root-directory", platform.RootDirectory)];
    }

    static IReadOnlyList<string?> TestApplicationParts(DotNetTestingPlatformConfig platform) =>
        [
            Arg("--report-trx", platform.ReportTrx),
            Arg("--report-trx-filename", platform.ReportTrxFileName),
            Arg("--report-html", platform.ReportHtml),
            Arg("--report-html-filename", platform.ReportHtmlFileName),
            Arg("--report-junit", platform.ReportJUnit),
            Arg("--report-junit-filename", platform.ReportJUnitFileName),
            Arg("--report-ctrf", platform.ReportCtrf),
            Arg("--report-ctrf-filename", platform.ReportCtrfFileName),
            Arg("--report-azdo", platform.ReportAzureDevOps),
            Arg("--report-azdo-severity", platform.ReportAzureDevOpsSeverity),
            Arg("--report-azdo-groups", OnOff(platform.ReportAzureDevOpsGroups)),
            Arg("--report-azdo-annotations", OnOff(platform.ReportAzureDevOpsAnnotations)),
            Arg("--report-azdo-flaky-history", platform.ReportAzureDevOpsFlakyHistory),
            Arg("--report-azdo-demote-known-flaky", platform.ReportAzureDevOpsDemoteKnownFlaky),
            Arg("--report-azdo-slow-test-history", platform.ReportAzureDevOpsSlowTestHistory),
            Arg("--report-azdo-slow-test-history-min-sample", platform.ReportAzureDevOpsSlowTestHistoryMinSample),
            Arg("--report-azdo-slow-test-history-multiplier", platform.ReportAzureDevOpsSlowTestHistoryMultiplier),
            Arg("--report-azdo-quarantine-file", platform.ReportAzureDevOpsQuarantineFile),
            platform.ReportAzureDevOpsSummaryPath is not null
                ? Arg("--report-azdo-summary", platform.ReportAzureDevOpsSummaryPath)
                : Arg("--report-azdo-summary", platform.ReportAzureDevOpsSummary),
            Args("--report-azdo-stackframe-filter", platform.ReportAzureDevOpsStackFrameFilters, " --report-azdo-stackframe-filter "),
            Arg("--report-azdo-upload-artifacts", platform.ReportAzureDevOpsUploadArtifacts?.ToString().ToKebabCaseLower()),
            Arg("--report-azdo-upload-artifact-include", platform.ReportAzureDevOpsUploadArtifactInclude),
            Arg("--report-azdo-upload-artifact-exclude", platform.ReportAzureDevOpsUploadArtifactExclude),
            Arg("--report-azdo-upload-artifact-name", platform.ReportAzureDevOpsUploadArtifactName),
            Arg("--publish-azdo-test-results", platform.PublishAzureDevOpsTestResults),
            Arg("--publish-azdo-run-name", platform.PublishAzureDevOpsRunName),
            Arg("--report-gh", platform.ReportGitHub),
            Arg("--report-gh-groups", OnOff(platform.ReportGitHubGroups)),
            Arg("--report-gh-annotations", OnOff(platform.ReportGitHubAnnotations)),
            Arg("--report-gh-step-summary", platform.ReportGitHubStepSummary?.ToString().ToKebabCaseLower()),
            Arg("--report-gh-step-summary-sections", platform.ReportGitHubStepSummarySections?.ToString().ToKebabCaseLower()),
            Arg("--report-gh-failure-details", OnOff(platform.ReportGitHubFailureDetails)),
            Arg("--report-gh-history", platform.ReportGitHubHistory),
            Arg("--report-gh-history-window", platform.ReportGitHubHistoryWindow),
            Arg("--report-gh-slow-test-notices", OnOff(platform.ReportGitHubSlowTestNotices)),
            Arg("--report-gh-slow-test-threshold", platform.ReportGitHubSlowTestThreshold),
        ];

    static string? OnOff(bool? value) =>
        value switch
            {
                true => "on",
                false => "off",
                null => null,
            };

    static string? RenderOutcomes(TestingPlatformOutcomes? outcomes) =>
        outcomes switch
            {
                null => null,
                TestingPlatformOutcomes.None => "none",
                TestingPlatformOutcomes.All => "all",
                { } selected => string.Join(',', new[] { TestingPlatformOutcomes.Passed, TestingPlatformOutcomes.Failed, TestingPlatformOutcomes.Skipped }
                    .Where(outcome => selected.HasFlag(outcome))
                    .Select(outcome => outcome.ToString().ToLowerInvariant())),
            };
}

/// <summary>Options for <c>dotnet test</c> in a repository that runs tests with VSTest.</summary>
public sealed record DotNetVSTestConfig
{
    /// <summary>Uses this run-settings file to configure the test run.</summary>
    public string? Settings { get; init; }
    /// <summary>Runs only tests matching the VSTest filter expression.</summary>
    public string? Filter { get; init; }
    /// <summary>Searches this directory for additional test adapters.</summary>
    public string? TestAdapterPath { get; init; }
    /// <summary>Enables test loggers, such as <c>trx</c>; each value may include semicolon-delimited logger settings.</summary>
    public IReadOnlyList<string> Loggers { get; init => field = value.ToArray(); } = [];
    /// <summary>Places command outputs in the specified directory.</summary>
    public string? Output { get; init; }
    /// <summary>Writes diagnostic test-platform logs to this file.</summary>
    public string? Diag { get; init; }
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
    /// <summary>Allows authentication and other operations to prompt for input.</summary>
    public bool Interactive { get; init; }
    /// <summary>Prevents reuse of persistent build servers during this invocation.</summary>
    public bool DisableBuildServers { get; init; }
}

/// <summary>Options for <c>dotnet test</c> in a repository that runs tests with Microsoft.Testing.Platform.</summary>
/// <remarks>
/// Report options belong to Microsoft.Testing.Platform extensions and are passed to the test applications after <c>--</c>.
/// Each test project must reference the extension package that provides a report option, or the test application
/// rejects it. Additional arguments follow the report options.
/// </remarks>
public sealed record DotNetTestingPlatformConfig
{
    /// <summary>Runs already-built test modules matching this glob expression instead of a solution or project.</summary>
    public string? TestModules { get; init; }
    /// <summary>The root directory used to resolve <see cref="TestModules"/>.</summary>
    public string? RootDirectory { get; init; }
    /// <summary>The maximum number of test modules that run in parallel.</summary>
    public int? MaxParallelTestModules { get; init; }
    /// <summary>The <c>testconfig.json</c> file used for test execution.</summary>
    public string? ConfigFile { get; init; }
    /// <summary>How a multi-module run organizes files under the results directory.</summary>
    public TestingPlatformResultsLayout? ResultsDirectoryLayout { get; init; }
    /// <summary>The directory where diagnostic output is stored.</summary>
    public string? DiagnosticOutputDirectory { get; init; }
    /// <summary>Fails the run when fewer tests than this run across all test modules.</summary>
    public int? MinimumExpectedTests { get; init; }
    /// <summary>Stops the run after this many failed, errored, timed-out, or canceled tests.</summary>
    public int? MaximumFailedTests { get; init; }
    /// <summary>Stops the run after a duration such as <c>90s</c>, <c>10m</c>, or <c>2h</c>.</summary>
    public string? Timeout { get; init; }
    /// <summary>Uses the current runtime as the target runtime during restore and build.</summary>
    public bool CurrentRuntime { get; init; }
    /// <summary>Skips building project-to-project references.</summary>
    public bool NoDependencies { get; init; }
    /// <summary>Disables ANSI escape characters in console output.</summary>
    public bool NoAnsi { get; init; }
    /// <summary>Disables progress reporting in console output.</summary>
    public bool NoProgress { get; init; }
    /// <summary>Disables combining compatible artifacts, such as reports, after a multi-module run.</summary>
    public bool NoArtifactPostProcessing { get; init; }
    /// <summary>The detail level of test result output.</summary>
    public TestingPlatformOutput? Output { get; init; }
    /// <summary>The test outcomes whose result blocks are shown; overrides <see cref="Output"/>.</summary>
    public TestingPlatformOutcomes? ShowTestResults { get; init; }
    /// <summary>Lists discovered tests in this format without executing them.</summary>
    public TestingPlatformListFormat? ListTestsFormat { get; init; }
    /// <summary>Ignores <c>launchSettings.json</c> when running test applications.</summary>
    public bool NoLaunchProfile { get; init; }
    /// <summary>Ignores launch-profile command-line arguments when running test applications.</summary>
    public bool NoLaunchProfileArguments { get; init; }
    /// <summary>The device, emulator, or simulator used for mobile or Apple-platform test projects.</summary>
    public string? Device { get; init; }
    /// <summary>Lists available devices for a project without running tests.</summary>
    public bool ListDevices { get; init; }
    /// <summary>Collects a repository test map; experimental and requires a separately distributed extension.</summary>
    public bool CollectTestMap { get; init; }
    /// <summary>Runs only tests affected by a change; experimental and requires a separately distributed extension.</summary>
    public bool AffectedTests { get; init; }

    /// <summary>Writes a TRX report; requires <c>Microsoft.Testing.Extensions.TrxReport</c>.</summary>
    public bool ReportTrx { get; init; }
    /// <summary>The TRX report file name, which may use placeholders such as <c>{asm}</c>, <c>{tfm}</c>, and <c>{arch}</c>.</summary>
    public string? ReportTrxFileName { get; init; }
    /// <summary>Writes an HTML report; experimental and requires <c>Microsoft.Testing.Extensions.HtmlReport</c>.</summary>
    public bool ReportHtml { get; init; }
    /// <summary>The HTML report file name, ending in <c>.html</c>.</summary>
    public string? ReportHtmlFileName { get; init; }
    /// <summary>Writes a JUnit XML report; experimental and requires <c>Microsoft.Testing.Extensions.JUnitReport</c>.</summary>
    public bool ReportJUnit { get; init; }
    /// <summary>The JUnit XML report file name, ending in <c>.xml</c>.</summary>
    public string? ReportJUnitFileName { get; init; }
    /// <summary>Writes a Common Test Report Format JSON report; experimental and requires <c>Microsoft.Testing.Extensions.CtrfReport</c>.</summary>
    public bool ReportCtrf { get; init; }
    /// <summary>The CTRF report file name, ending in <c>.json</c>.</summary>
    public string? ReportCtrfFileName { get; init; }

    /// <summary>Reports errors, warnings, and test annotations to Azure Pipelines; requires <c>Microsoft.Testing.Extensions.AzureDevOpsReport</c>.</summary>
    public bool ReportAzureDevOps { get; init; }
    /// <summary>The severity used for reported Azure Pipelines events.</summary>
    public TestingPlatformAzureDevOpsSeverity? ReportAzureDevOpsSeverity { get; init; }
    /// <summary>Whether each test assembly's output appears in a collapsible Azure Pipelines log group.</summary>
    public bool? ReportAzureDevOpsGroups { get; init; }
    /// <summary>Whether failed and skipped tests produce Azure Pipelines annotations.</summary>
    public bool? ReportAzureDevOpsAnnotations { get; init; }
    /// <summary>The number of days, from 1 through 90, of Azure DevOps test history used to annotate flaky failures.</summary>
    public int? ReportAzureDevOpsFlakyHistory { get; init; }
    /// <summary>Reports failures that are flaky in the Azure DevOps history window as warnings.</summary>
    public bool ReportAzureDevOpsDemoteKnownFlaky { get; init; }
    /// <summary>The number of days, from 1 through 90, of Azure DevOps test history used to tune slow-test thresholds.</summary>
    public int? ReportAzureDevOpsSlowTestHistory { get; init; }
    /// <summary>The minimum number of historical samples required before a test's history adjusts its slow-test threshold.</summary>
    public int? ReportAzureDevOpsSlowTestHistoryMinSample { get; init; }
    /// <summary>The multiplier applied to a test's historical p99 duration to calculate its slow-test threshold.</summary>
    public double? ReportAzureDevOpsSlowTestHistoryMultiplier { get; init; }
    /// <summary>A text file listing quarantined test names or glob patterns whose failures are reported as warnings.</summary>
    public string? ReportAzureDevOpsQuarantineFile { get; init; }
    /// <summary>Writes and uploads a Markdown job summary at the end of the test run.</summary>
    public bool ReportAzureDevOpsSummary { get; init; }
    /// <summary>Writes and uploads the Markdown job summary at this path instead of the default location.</summary>
    public string? ReportAzureDevOpsSummaryPath { get; init; }
    /// <summary>Regular expressions for stack-frame type prefixes skipped when locating the call site to annotate.</summary>
    public IReadOnlyList<string> ReportAzureDevOpsStackFrameFilters { get; init => field = value.ToArray(); } = [];
    /// <summary>Uploads test result files, adds build tags, or both.</summary>
    public TestingPlatformAzureDevOpsUpload? ReportAzureDevOpsUploadArtifacts { get; init; }
    /// <summary>A glob, relative to the results directory, selecting files to upload.</summary>
    public string? ReportAzureDevOpsUploadArtifactInclude { get; init; }
    /// <summary>A glob, relative to the results directory, excluding files from upload.</summary>
    public string? ReportAzureDevOpsUploadArtifactExclude { get; init; }
    /// <summary>The Azure Pipelines artifact container name used for uploads.</summary>
    public string? ReportAzureDevOpsUploadArtifactName { get; init; }
    /// <summary>Streams results to an Azure DevOps test run shown on the build's Tests tab; requires <c>SYSTEM_ACCESSTOKEN</c>.</summary>
    public bool PublishAzureDevOpsTestResults { get; init; }
    /// <summary>The Azure DevOps test run name used when publishing results.</summary>
    public string? PublishAzureDevOpsRunName { get; init; }

    /// <summary>Reports log groups, test annotations, a job summary, and slow-test notices to GitHub Actions; requires <c>Microsoft.Testing.Extensions.GitHubActionsReport</c>.</summary>
    /// <remarks>The reporter does nothing outside GitHub Actions, so it can stay enabled for local builds.</remarks>
    public bool ReportGitHub { get; init; }
    /// <summary>Whether each test assembly's output appears in a collapsible GitHub Actions log group.</summary>
    public bool? ReportGitHubGroups { get; init; }
    /// <summary>Whether failed and skipped tests produce GitHub Actions annotations.</summary>
    public bool? ReportGitHubAnnotations { get; init; }
    /// <summary>When the Markdown job summary is written.</summary>
    public TestingPlatformGitHubStepSummary? ReportGitHubStepSummary { get; init; }
    /// <summary>The content included in the Markdown job summary.</summary>
    public TestingPlatformGitHubSummarySections? ReportGitHubStepSummarySections { get; init; }
    /// <summary>Whether the job summary includes bounded failure details.</summary>
    public bool? ReportGitHubFailureDetails { get; init; }
    /// <summary>A test-history snapshot file that the workflow restores before and saves after the run.</summary>
    public string? ReportGitHubHistory { get; init; }
    /// <summary>The number of days, from 1 through 90, retained in the test-history snapshot.</summary>
    public int? ReportGitHubHistoryWindow { get; init; }
    /// <summary>Whether slow tests produce GitHub Actions notices.</summary>
    public bool? ReportGitHubSlowTestNotices { get; init; }
    /// <summary>The duration, such as <c>90s</c> or <c>2m</c>, after which a running test produces a slow-test notice.</summary>
    public string? ReportGitHubSlowTestThreshold { get; init; }
}

/// <summary>How a Microsoft.Testing.Platform multi-module run organizes the results directory.</summary>
public enum TestingPlatformResultsLayout
{
    /// <summary>Writes all results to the same directory.</summary>
    Flat,
    /// <summary>Writes each module's results to its own subdirectory.</summary>
    PerModule,
}

/// <summary>The detail level of Microsoft.Testing.Platform test result output.</summary>
public enum TestingPlatformOutput
{
    /// <summary>Shows the least detail.</summary>
    Minimal,
    /// <summary>Shows the default detail.</summary>
    Normal,
    /// <summary>Shows the most detail.</summary>
    Detailed,
}

/// <summary>Test outcomes whose Microsoft.Testing.Platform result blocks are shown.</summary>
[Flags]
public enum TestingPlatformOutcomes
{
    /// <summary>Shows no result blocks.</summary>
    None = 0,
    /// <summary>Shows passed tests.</summary>
    Passed = 1,
    /// <summary>Shows failed, errored, timed-out, and canceled tests.</summary>
    Failed = 2,
    /// <summary>Shows skipped tests.</summary>
    Skipped = 4,
    /// <summary>Shows every outcome.</summary>
    All = Passed | Failed | Skipped,
}

/// <summary>The format used to list discovered Microsoft.Testing.Platform tests.</summary>
public enum TestingPlatformListFormat
{
    /// <summary>Human-readable text.</summary>
    Text,
    /// <summary>A versioned JSON document.</summary>
    Json,
}

/// <summary>The severity used for Azure Pipelines test events.</summary>
public enum TestingPlatformAzureDevOpsSeverity
{
    /// <summary>Reports events as errors.</summary>
    Error,
    /// <summary>Reports events as warnings.</summary>
    Warning,
}

/// <summary>What the Azure DevOps reporter uploads to the build.</summary>
public enum TestingPlatformAzureDevOpsUpload
{
    /// <summary>Uploads nothing.</summary>
    Off,
    /// <summary>Adds build tags only.</summary>
    TagsOnly,
    /// <summary>Uploads test result files only.</summary>
    Files,
    /// <summary>Uploads test result files and adds build tags.</summary>
    All,
}

/// <summary>When the GitHub Actions reporter writes a Markdown job summary.</summary>
public enum TestingPlatformGitHubStepSummary
{
    /// <summary>Always writes the summary.</summary>
    On,
    /// <summary>Never writes the summary.</summary>
    Off,
    /// <summary>Writes the summary only when the run fails.</summary>
    OnFailure,
}

/// <summary>Content included in the GitHub Actions job summary.</summary>
public enum TestingPlatformGitHubSummarySections
{
    /// <summary>Test results only.</summary>
    TestResults,
    /// <summary>Slow tests only.</summary>
    SlowTests,
    /// <summary>Code coverage only.</summary>
    Coverage,
    /// <summary>Every section.</summary>
    All,
}

/// <summary>Detects the repository's <c>dotnet test</c> runner mode the same way the .NET SDK does.</summary>
static class DotNetTestRunner
{
    const string TestingPlatform = "Microsoft.Testing.Platform";

    static readonly JsonDocumentOptions Options = new() { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip };

    public static bool UsesTestingPlatform(AbsolutePath directory)
    {
        if (Parse(Environment.GetEnvironmentVariable("DOTNET_TEST_RUNNER")) is { } fromEnvironment)
            return fromEnvironment;

        for (var current = directory; ; current = current.Parent)
        {
            var globalJson = current / "global.json";
            if (globalJson.IsExistingFile)
                return ReadRunner(globalJson);
            if (current.IsRoot)
                return false;
        }
    }

    static bool ReadRunner(AbsolutePath globalJson)
    {
        using var document = JsonDocument.Parse(globalJson.ReadText(), Options);
        return document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty("test", out var test)
            && test.ValueKind == JsonValueKind.Object
            && test.TryGetProperty("runner", out var runner)
            && runner.ValueKind == JsonValueKind.String
            && Parse(runner.GetString()) == true;
    }

    static bool? Parse(string? runner) =>
        runner?.Trim() switch
            {
                { } value when value.Equals(TestingPlatform, StringComparison.OrdinalIgnoreCase) => true,
                { } value when value.Equals("VSTest", StringComparison.OrdinalIgnoreCase) => false,
                _ => null,
            };
}
