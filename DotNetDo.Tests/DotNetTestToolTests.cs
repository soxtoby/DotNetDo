using Xunit;

namespace DotNetDo.Tests;

public sealed class DotNetTestToolTests : IDisposable
{
    readonly AbsolutePath _directory = AbsolutePath.Parse(Path.Combine(Path.GetTempPath(), $"dotnetdo-{Guid.NewGuid():N}"));

    public DotNetTestToolTests() => _directory.EnsureDirectoryExists();

    public void Dispose() => Directory.Delete(_directory, true);

    DotNetTest Command(params string[] targets) =>
        Tools.DotNet.Test with { Targets = targets, Verbosity = null, Configuration = null, WorkingDirectory = _directory };

    void UseTestingPlatform(AbsolutePath? directory = null) =>
        File.WriteAllText(
            (directory ?? _directory) / "global.json",
            """
            {
              // Opt into the Microsoft.Testing.Platform mode of dotnet test.
              "sdk": { "version": "10.0.100" },
              "test": { "runner": "microsoft.testing.platform" },
            }
            """);

    [Fact]
    public void VSTest_mode_renders_shared_options_before_VSTest_options()
    {
        var command = Command("Product.Tests.csproj") with
            {
                ListTests = true,
                TestEnvironment = ["DEPLOYMENT_SLOT=Release Candidate"],
                ResultsDirectory = "test results",
                Configuration = "Release",
                VSTest = new()
                    {
                        Settings = "CI.runsettings",
                        Filter = "Category=Unit",
                        Loggers = ["trx;LogFileName=CI Results.trx"],
                        Output = "test output",
                        BlameHang = true,
                        BlameHangTimeout = "2m",
                        DisableBuildServers = true,
                    },
            };

        Assert.Equal(
            "dotnet test Product.Tests.csproj --list-tests --environment \"DEPLOYMENT_SLOT=Release Candidate\" --results-directory \"test results\" --configuration Release --settings CI.runsettings --filter Category=Unit --logger \"trx;LogFileName=CI Results.trx\" --output \"test output\" --blame-hang --blame-hang-timeout 2m --disable-build-servers",
            command.ToString());
    }

    [Fact]
    public void VSTest_mode_rejects_testing_platform_options()
    {
        var command = Command("Product.slnx") with { TestingPlatform = new() { ReportTrx = true } };

        var exception = Assert.Throws<InvalidOperationException>(() => command.ToString());
        Assert.Contains("VSTest", exception.Message);
    }

    [Fact]
    public void Testing_platform_mode_renders_selection_options_and_reports_after_separator()
    {
        UseTestingPlatform();
        var command = Command("Product.slnx") with
            {
                ListTests = true,
                ResultsDirectory = "test results",
                AdditionalArguments = "--coverage",
                TestingPlatform = new()
                    {
                        ListTestsFormat = TestingPlatformListFormat.Json,
                        MinimumExpectedTests = 10,
                        ResultsDirectoryLayout = TestingPlatformResultsLayout.PerModule,
                        Output = TestingPlatformOutput.Detailed,
                        ShowTestResults = TestingPlatformOutcomes.Failed | TestingPlatformOutcomes.Skipped,
                        ReportTrx = true,
                        ReportTrxFileName = "{asm}_{tfm}.trx",
                        ReportJUnit = true,
                        ReportAzureDevOps = true,
                        ReportAzureDevOpsSeverity = TestingPlatformAzureDevOpsSeverity.Warning,
                        ReportAzureDevOpsGroups = false,
                        ReportAzureDevOpsSlowTestHistoryMultiplier = 1.5,
                        ReportAzureDevOpsSummary = true,
                        ReportAzureDevOpsStackFrameFilters = ["^Product\\.Assertions", "^Shouldly"],
                        ReportAzureDevOpsUploadArtifacts = TestingPlatformAzureDevOpsUpload.TagsOnly,
                        ReportGitHub = true,
                        ReportGitHubStepSummary = TestingPlatformGitHubStepSummary.OnFailure,
                        ReportGitHubStepSummarySections = TestingPlatformGitHubSummarySections.TestResults,
                        ReportGitHubSlowTestNotices = true,
                    },
            };

        Assert.Equal(
            "dotnet test --solution Product.slnx --list-tests json --results-directory \"test results\" --results-directory-layout per-module --minimum-expected-tests 10 --output Detailed --show-test-results failed,skipped -- --report-trx --report-trx-filename {asm}_{tfm}.trx --report-junit --report-azdo --report-azdo-severity warning --report-azdo-groups off --report-azdo-slow-test-history-multiplier 1.5 --report-azdo-summary --report-azdo-stackframe-filter ^Product\\.Assertions --report-azdo-stackframe-filter ^Shouldly --report-azdo-upload-artifacts tags-only --report-gh --report-gh-step-summary on-failure --report-gh-step-summary-sections test-results --report-gh-slow-test-notices on --coverage",
            command.ToString());
    }

    [Fact]
    public void Testing_platform_mode_renders_a_bare_command_without_a_separator()
    {
        UseTestingPlatform();

        Assert.Equal("dotnet test --project \"src/Product Tests.csproj\"", Command("src/Product Tests.csproj").ToString());
        Assert.Equal("dotnet test --list-tests", (Command() with { ListTests = true }).ToString());
        Assert.Equal(
            "dotnet test --project Product.Tests.csproj -- --report-azdo-summary azdo.md",
            (Command("Product.Tests.csproj") with { TestingPlatform = new() { ReportAzureDevOpsSummaryPath = "azdo.md" } }).ToString());
    }

    [Fact]
    public void Testing_platform_test_modules_replace_targets()
    {
        UseTestingPlatform();
        var command = Command("Product.slnx", "Other.slnx") with
            {
                TestingPlatform = new() { TestModules = "**/bin/**/*.Tests.dll", RootDirectory = "artifacts" },
            };

        Assert.Equal("dotnet test --test-modules **/bin/**/*.Tests.dll --root-directory artifacts", command.ToString());
    }

    [Fact]
    public void Testing_platform_mode_rejects_unsupported_targets_and_VSTest_options()
    {
        UseTestingPlatform();

        Assert.Throws<InvalidOperationException>(() => Command("Product.slnx", "Other.slnx").ToString());
        Assert.Throws<InvalidOperationException>(() => Command("tests").ToString());
        var exception = Assert.Throws<InvalidOperationException>(() => (Command("Product.slnx") with { VSTest = new() { Loggers = ["trx"] } }).ToString());
        Assert.Contains("Microsoft.Testing.Platform", exception.Message);
    }

    [Fact]
    public void Nearest_global_json_selects_the_runner_mode()
    {
        UseTestingPlatform();
        var nested = (_directory / "nested").EnsureDirectoryExists();
        File.WriteAllText(nested / "global.json", """{ "sdk": { "version": "10.0.100" } }""");
        var child = (nested / "child").EnsureDirectoryExists();

        Assert.Equal("dotnet test --solution Product.slnx", (Command("Product.slnx")).ToString());
        Assert.Equal("dotnet test Product.slnx", (Command("Product.slnx") with { WorkingDirectory = child }).ToString());
    }
}
