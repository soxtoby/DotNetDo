using Serilog.Events;
using Xunit;

namespace DotNetDo.Tests;

public sealed class NuGetToolTests
{
    [Fact]
    public void Config_snapshots_settings_and_renders_null_as_removal()
    {
        var settings = new Dictionary<string, string?> { ["repositoryPath"] = null };
        var command = Tools.NuGet.Config with { Settings = settings };
        settings["repositoryPath"] = "changed";

        Assert.Equal("nuget config -Set repositoryPath= -Verbosity normal", command.ToString());
    }

    [Fact]
    public void Install_leaves_conflicting_targets_to_nuget()
    {
        var path = AbsolutePath.Parse(Path.Combine(Path.GetTempPath(), "packages.config"));
        var command = Tools.NuGet.Install with { PackageId = "Example", PackagesConfigPath = path };

        Assert.Equal($"nuget install Example {path.QuotedArgument()} -Verbosity normal", command.ToString());
    }

    [Fact]
    public void Pack_keeps_additional_arguments_after_properties()
    {
        var path = AbsolutePath.Parse(Path.Combine(Path.GetTempPath(), "example.nuspec"));
        var command = Tools.NuGet.Pack with
        {
            InputPath = path,
            Properties = new Dictionary<string, string?> { ["Configuration"] = "Release" },
            AdditionalArguments = "-NoPackageAnalysis",
        };

        Assert.Equal($"nuget pack {path.QuotedArgument()} -Verbosity normal -Properties Configuration=Release -NoPackageAnalysis", command.ToString());
    }

    [Fact]
    public void Required_operands_fail_before_execution()
    {
        Assert.Throws<InvalidOperationException>(() => Tools.NuGet.Add.ToString());
        Assert.Throws<InvalidOperationException>(() => Tools.NuGet.Verify.ToString());
        Assert.Throws<InvalidOperationException>(() => (Tools.NuGet.Search with { Verbosity = (NuGetVerbosity)99 }).ToString());
    }

    [Fact]
    public void Fresh_commands_snapshot_logging_verbosity()
    {
        var original = Logging.Level;
        try
        {
            Logging.Level = LogEventLevel.Debug;
            var detailed = Tools.NuGet.Search;
            Logging.Level = LogEventLevel.Warning;

            Assert.Equal(NuGetVerbosity.Detailed, detailed.Verbosity);
            Assert.Equal(NuGetVerbosity.Quiet, Tools.NuGet.Search.Verbosity);
            Assert.NotSame(Tools.NuGet.Search, Tools.NuGet.Search);
        }
        finally { Logging.Level = original; }
    }
}
