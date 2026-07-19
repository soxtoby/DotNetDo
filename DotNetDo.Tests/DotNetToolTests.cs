using Xunit;

namespace DotNetDo.Tests;

public sealed class DotNetToolTests
{
    [Fact]
    public async Task Uses_Do_Solution_as_the_default_target()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"dotnetdo-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "Product.slnx");
        await File.WriteAllTextAsync(path, "<Solution />", TestContext.Current.CancellationToken);
        var original = Do.Solution;

        try
        {
            Do.Solution = await Solution.Load(path, TestContext.Current.CancellationToken);

            Assert.Equal($"dotnet build {path.QuotedArgument()}", Tools.DotNet.Build.ToString());
            var command = Tools.DotNet.Test with { Targets = ["My App.csproj"], Output = "test output" };
            Assert.Equal(["My App.csproj"], command.Targets);
            Assert.Equal("test output", command.Output);
            Assert.Equal("dotnet test \"My App.csproj\" --output \"test output\"", command.ToString());
        }
        finally
        {
            Do.Solution = original;
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Tool_commands_quote_semantic_values_during_rendering()
    {
        var values = new List<string> { "one value", "" };
        var entries = new Dictionary<string, string> { ["first=key"] = "first=value" };
        var command = new TestToolCommand
        {
            Value = "scalar value",
            Values = values,
            Entries = entries,
            Raw = "--first second",
            Blank = "   ",
        };
        values[0] = "changed";
        entries["first=key"] = "changed";

        Assert.Equal("scalar value", command.Value);
        Assert.Equal(["one value", ""], command.Values);
        Assert.Equal("first=value", command.Entries["first=key"]);
        Assert.Equal("--first second", command.Raw);
        Assert.Equal("   ", command.Blank);
        Assert.Equal("example --value \"scalar value\" --values \"one value\" --entries \"first=key=first=value\" --raw --first second", command.ToString());

        var replacement = command with { Blank = "later value" };
        Assert.Equal("example --value \"scalar value\" --values \"one value\" --entries \"first=key=first=value\" --raw --first second --blank \"later value\"", replacement.ToString());

        var preQuoted = command with { Value = "scalar value".QuotedArgument() };
        Assert.Equal("example --value \"\\\"scalar value\\\"\" --values \"one value\" --entries \"first=key=first=value\" --raw --first second", preQuoted.ToString());
    }

    [Fact]
    public void Renders_dotnet_nuget_push()
    {
        var command = Tools.DotNet.NuGetPush with
        {
            Package = "artifacts/My Package.nupkg",
            Source = "https://api.nuget.org/v3/index.json",
            ApiKey = "secret key",
            SkipDuplicate = true,
            Timeout = TimeSpan.FromMinutes(6),
        };

        Assert.Equal(TimeSpan.FromMinutes(6), command.Timeout);
        Assert.Equal("dotnet nuget push \"artifacts/My Package.nupkg\" --source https://api.nuget.org/v3/index.json --api-key \"secret key\" --skip-duplicate --timeout 360", command.ToString());
        var fractionalTimeout = Tools.DotNet.NuGetPush with { Timeout = TimeSpan.FromMilliseconds(1500) };
        Assert.Equal("dotnet nuget push --timeout 1", fractionalTimeout.ToString());
    }

    [Fact]
    public void Renders_msbuild_with_located_toolset_and_typed_options()
    {
        var command = Tools.MSBuild with
        {
            Projects = ["My App.csproj"],
            Targets = ["Clean", "Compile"],
            Properties = new Dictionary<string, string> { ["Configuration"] = "Release Candidate" },
            Verbosity = MSBuildVerbosity.Detailed,
            MaxCpuCount = 4,
            Restore = true,
            NoLogo = true,
            NodeReuse = false,
        };

        Assert.Equal(["My App.csproj"], command.Projects);
        Assert.Equal(["Clean", "Compile"], command.Targets);
        Assert.Equal("Release Candidate", command.Properties["Configuration"]);
        Assert.Equal("Release Candidate", command.Properties["configuration"]);
        Assert.Matches("^(?:\".*MSBuild\\.exe\"|dotnet \".*MSBuild\\.dll\") ", command.ToString());
        Assert.EndsWith("\"My App.csproj\" -target:Clean;Compile -property:\"Configuration=Release Candidate\" -verbosity:detailed -maxCpuCount:4 -restore -noLogo -nodeReuse:false", command.ToString());
    }

    [Fact]
    public void MSBuild_defaults_are_fresh_and_target_Do_Solution()
    {
        var first = Tools.MSBuild;
        var second = Tools.MSBuild;

        Assert.NotSame(first, second);
        Assert.Equal([Do.Solution.Path], first.Projects);
        Assert.EndsWith(Do.Solution.Path.QuotedArgument(), first.ToString());
    }

    sealed record TestToolCommand : ExecToolCommand
    {
        protected override string CommandPrefix => "example";
        public string? Value { get => GetArgument("value"); init => SetArgument("value", "--value ", value); }
        public IReadOnlyList<string> Values { get => GetArgumentArray("values"); init => SetArgumentArray("values", "--values ", value); }
        public IReadOnlyDictionary<string, string> Entries { get => GetArgumentDictionary("entries"); init => SetArgumentDictionary("entries", "--entries ", value); }
        public string? Raw { get => GetArgument("raw"); init => SetArgument("raw", "--raw ", value, quote: false); }
        public string? Blank { get => GetArgument("blank"); init => SetArgument("blank", "--blank ", value); }
    }
}
