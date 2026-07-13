using Xunit;

namespace DotNetDo.Tests;

public sealed class PackageToolTests
{
    [Fact]
    public void Renders_one_off_package_tools()
    {
        var command = new PackageToolCommand("Example.Tool", "example")
            { AdditionalArguments = "--value 1" };

        Assert.Equal("dotnet tool run example --value 1", command.ToString());
        Assert.Equal("Example.Tool", command.PackageId);
        Assert.Equal("example", command.CommandName);
    }

    [Fact]
    public void Renders_dotnet_tool_restore_options()
    {
        var command = Tools.DotNet.ToolRestore with
        {
            AddSources = ["private", "mirror"],
            DisableParallel = true,
            Interactive = true,
            Verbosity = "minimal",
        };

        Assert.Equal("dotnet tool restore --add-source private --add-source mirror --disable-parallel --interactive --verbosity minimal", command.ToString());
    }

    [Fact]
    public void GitVersion_forces_json_and_round_trip_dates()
    {
        var target = AbsolutePath.Parse(Path.Combine(Path.GetTempPath(), "repository with spaces"));
        var command = Tools.GitVersion with
        {
            TargetPath = target,
            NoFetch = true,
            OverrideConfig = new Dictionary<string, string> { ["tag-prefix"] = "release/" },
        };

        Assert.Equal(
            $"dotnet tool run dotnet-gitversion {target.QuotedArgument()} -output json -overrideconfig commit-date-format=O -nofetch -overrideconfig tag-prefix=release/",
            command.ToString());
        Assert.Throws<ArgumentException>(() => Tools.GitVersion with
        {
            OverrideConfig = new Dictionary<string, string> { ["commit-date-format"] = "yyyy-MM-dd" },
        });
    }

    [Fact]
    public void Parses_all_known_fields_and_preserves_unknown_fields()
    {
        var result = new ExecResult
        {
            Command = "gitversion",
            WorkingDirectory = "work",
            ExitCode = 0,
            Output =
            [
                new ExecOutput(OutputType.Out, "{\"Major\":1,\"Minor\":2,\"Patch\":3,\"SemVer\":\"1.2.3\",\"CommitDate\":\"2026-07-12T10:11:12.0000000+10:00\",\"Future\":true}"),
            ],
        };

        var parsed = GitVersionResult.Parse(result);

        Assert.Equal("1.2.3", parsed.SemVer);
        Assert.Equal(TimeSpan.FromHours(10), parsed.CommitDate.Offset);
        Assert.True(parsed.AdditionalVariables["Future"].GetBoolean());
    }

    [Fact]
    public void Invalid_semantic_output_preserves_the_raw_result()
    {
        var result = new ExecResult
        {
            Command = "gitversion",
            WorkingDirectory = "work",
            ExitCode = 0,
            Output = [new ExecOutput(OutputType.Out, "not json")],
        };

        var exception = Assert.Throws<ToolOutputException>(() => GitVersionResult.Parse(result));

        Assert.Same(result, exception.Result);
        Assert.Equal(typeof(GitVersionResult), exception.ExpectedType);
    }
}
