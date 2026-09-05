using System.Collections.ObjectModel;
using System.Collections.Immutable;
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

            var build = Tools.DotNet.Build with { Configuration = null, ContinuousIntegrationBuild = null };
            Assert.Equal($"dotnet build {path.QuotedArgument()} --verbosity normal", build.ToString());
            var command = Tools.DotNet.Test with { Targets = ["My App.csproj"], Output = "test output", Configuration = null };
            Assert.Equal(["My App.csproj"], command.Targets);
            Assert.Equal("test output", command.Output);
            Assert.Equal("dotnet test \"My App.csproj\" --verbosity normal --output \"test output\"", command.ToString());
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
        Assert.Equal("example --value \"scalar value\" --values \"one value\" --entries first=key=first=value --raw --first second", command.ToString());

        var reordered = new TestToolCommand
        {
            Blank = "   ",
            Raw = "--first second",
            Entries = new Dictionary<string, string> { ["first=key"] = "first=value" },
            Values = ["one value", ""],
            Value = "scalar value",
        };
        Assert.Equal(command.ToString(), reordered.ToString());

        var replacement = command with { Blank = "later value" };
        Assert.Equal("example --value \"scalar value\" --values \"one value\" --entries first=key=first=value --raw --first second --blank \"later value\"", replacement.ToString());

        var preQuoted = command with { Value = "scalar value".QuotedArgument() };
        Assert.Equal("example --value \"\\\"scalar value\\\"\" --values \"one value\" --entries first=key=first=value --raw --first second", preQuoted.ToString());
    }

    [Fact]
    public void Dotnet_test_distinguishes_process_and_test_environments()
    {
        Func<ImmutableDictionary<string, string>, ImmutableDictionary<string, string>> processEnvironment = environment => environment;
        var command = Tools.DotNet.Test with
            {
                Targets = [],
                Configuration = null,
                Environment = processEnvironment,
                TestEnvironment = ["DEPLOYMENT_SLOT=Release Candidate"],
            };

        Assert.Same(processEnvironment, command.Environment);
        Assert.Equal(["DEPLOYMENT_SLOT=Release Candidate"], command.TestEnvironment);
        Assert.Contains("--environment \"DEPLOYMENT_SLOT=Release Candidate\"", command.ToString());
    }

    [Fact]
    public void Argument_prefix_suffix_controls_value_spacing()
    {
        Assert.Equal(
            "example --space value --colon:value --equals=value -Wl,value",
            new PrefixToolCommand().ToString());
    }

    [Fact]
    public void Dotnet_build_renders_typed_and_additional_MSBuild_properties()
    {
        var properties = new Dictionary<string, string>
            {
                ["BuildLabel"] = "release, 100%; stable",
                [""] = "",
            };
        var command = Tools.DotNet.Build with
            {
                Targets = ["Product.csproj"],
                Configuration = null,
                ContinuousIntegrationBuild = false,
                Version = "1.2.3",
                VersionPrefix = "1.2",
                PackageVersion = "1.2.3-package",
                AssemblyVersion = "1.2.3.0",
                FileVersion = "1.2.3.4",
                InformationalVersion = "1.2.3+sha",
                Copyright = "Example, Inc.; 2026%",
                Properties = properties,
                AdditionalArguments = "--raw-property=value",
            };
        properties["BuildLabel"] = "changed";

        Assert.Equal("release, 100%; stable", command.Properties["BuildLabel"]);
        Assert.Equal(
            "dotnet build Product.csproj --verbosity normal --property:ContinuousIntegrationBuild=false --property:Version=1.2.3 --property:VersionPrefix=1.2 --property:PackageVersion=1.2.3-package --property:AssemblyVersion=1.2.3.0 --property:FileVersion=1.2.3.4 --property:InformationalVersion=1.2.3+sha --property:\"Copyright=Example%2C Inc.%3B 2026%25\" --property:\"BuildLabel=release%2C 100%25%3B stable\" --property:= --raw-property=value",
            command.ToString());
    }

    [Fact]
    public void Dotnet_pack_preserves_version_alias_and_renders_common_MSBuild_properties()
    {
        var command = Tools.DotNet.Pack with
            {
                Targets = ["Product.csproj"],
                Configuration = null,
                ContinuousIntegrationBuild = null,
                Version = "1.2.3",
                Copyright = "Example",
            };

        Assert.Equal(
            "dotnet pack Product.csproj --verbosity normal --version 1.2.3 --property:Copyright=Example",
            command.ToString());
    }

    [Theory]
    [InlineData("configuration", "Release", "Configuration")]
    [InlineData("RuntimeIdentifier", "linux-x64", "RuntimeIdentifier")]
    [InlineData("VERSION", "1.2.3", "Version")]
    [InlineData("TargetFramework", "net10.0", "TargetFramework")]
    [InlineData("SelfContained", "false", "SelfContained")]
    [InlineData("copyright", "Example", "Copyright")]
    public void Dotnet_build_rejects_typed_MSBuild_property_collisions(string propertyName, string propertyValue, string expectedName)
    {
        var command = Tools.DotNet.Build with
            {
                Targets = [],
                Configuration = propertyName.Equals("configuration", StringComparison.OrdinalIgnoreCase) ? "Release" : null,
                Runtime = propertyName.Equals("RuntimeIdentifier", StringComparison.OrdinalIgnoreCase) ? "linux-x64" : null,
                Version = propertyName.Equals("Version", StringComparison.OrdinalIgnoreCase) ? "1.2.3" : null,
                Framework = propertyName.Equals("TargetFramework", StringComparison.OrdinalIgnoreCase) ? "net10.0" : null,
                SelfContained = propertyName.Equals("SelfContained", StringComparison.OrdinalIgnoreCase) ? false : null,
                Copyright = propertyName.Equals("Copyright", StringComparison.OrdinalIgnoreCase) ? "Example" : null,
                ContinuousIntegrationBuild = null,
                Properties = new Dictionary<string, string> { [propertyName] = propertyValue },
            };

        var exception = Assert.Throws<InvalidOperationException>(() => command.ToString());
        Assert.Contains(expectedName, exception.Message);
    }

    [Fact]
    public void Dotnet_build_reports_every_typed_MSBuild_property_collision()
    {
        var command = Tools.DotNet.Build with
            {
                Targets = [],
                Configuration = "Release",
                ContinuousIntegrationBuild = true,
                Properties = new Dictionary<string, string>
                    {
                        ["configuration"] = "Debug",
                        ["CONTINUOUSINTEGRATIONBUILD"] = "false",
                    },
            };

        var exception = Assert.Throws<InvalidOperationException>(() => command.ToString());
        Assert.Contains("Configuration", exception.Message);
        Assert.Contains("ContinuousIntegrationBuild", exception.Message);
    }

    [Fact]
    public void Dotnet_build_leaves_additional_MSBuild_property_validation_to_MSBuild()
    {
        var command = Tools.DotNet.Build with
            {
                Targets = [],
                Configuration = null,
                ContinuousIntegrationBuild = null,
                Properties = new Dictionary<string, string>
                    {
                        ["label"] = "first",
                        ["LABEL"] = "second",
                        [""] = null!,
                    },
            };

        Assert.EndsWith("--property:label=first --property:LABEL=second --property:=", command.ToString());
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

        var reordered = Tools.DotNet.NuGetPush with
        {
            Timeout = TimeSpan.FromMinutes(6),
            SkipDuplicate = true,
            ApiKey = "secret key",
            Source = "https://api.nuget.org/v3/index.json",
            Package = "artifacts/My Package.nupkg",
        };
        Assert.Equal(command.ToString(), reordered.ToString());
    }

    [Fact]
    public void Renders_dotnet_package_search_and_reads_json_result()
    {
        var command = Tools.DotNet.PackageSearch with
            {
                SearchTerm = "My.Package",
                Sources = ["private feed", "https://api.nuget.org/v3/index.json"],
                ExactMatch = true,
                Prerelease = true,
                ConfigFile = "NuGet.Config",
                Verbosity = "minimal",
            };

        Assert.Equal(
            "dotnet package search My.Package --source \"private feed\" --source https://api.nuget.org/v3/index.json --exact-match --prerelease --configfile NuGet.Config --format json --verbosity minimal",
            command.ToString());

        var result = DotNetPackageSearchResult.Parse(new ExecResult
            {
                Command = command.ToString(),
                WorkingDirectory = "work",
                ExitCode = 0,
                AllOutput =
                    [
                        new(
                            OutputType.Out,
                            """{"version":2,"problems":[],"searchResult":[{"sourceName":"nuget.org","packages":[{"id":"My.Package","version":"1.0.0"},{"id":"My.Package","version":"2.0.0"}]}]}""")
                    ],
            });

        Assert.Equal(2, result.Version);
        Assert.Empty(result.Problems);
        Assert.Equal("nuget.org", Assert.Single(result.Sources).Name);
        var package = result.Sources[0].Packages.Last();
        Assert.Equal("My.Package", package.Id);
        Assert.Equal("2.0.0", package.Version);
    }

    [Fact]
    public void Renders_dotnet_tool_update()
    {
        var command = Tools.DotNet.ToolUpdate with
            {
                Package = "DotNetDo",
                ToolManifest = ".config/dotnet-tools.json",
                AddSources = ["private feed"],
                Prerelease = true,
                NoHttpCache = true,
                Verbosity = "minimal",
            };

        Assert.Equal(
            "dotnet tool update DotNetDo --tool-manifest .config/dotnet-tools.json --add-source \"private feed\" --prerelease --no-http-cache --verbosity minimal",
            command.ToString());
    }

    [Fact]
    public void Custom_format_command_deterministically_overrides_typed_command()
    {
        var typedFirst = Tools.DotNet.Format with
        {
            Command = FormatCommand.Whitespace,
            CustomCommand = "future-command",
        };
        var customFirst = Tools.DotNet.Format with
        {
            CustomCommand = "future-command",
            Command = FormatCommand.Whitespace,
        };

        Assert.Equal(typedFirst.ToString(), customFirst.ToString());
        Assert.StartsWith("dotnet format future-command ", typedFirst.ToString());
        Assert.DoesNotContain(" whitespace ", typedFirst.ToString());
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
        Assert.Matches("^(?:\"[^\"]*MSBuild\\.exe\"|\\S*MSBuild\\.exe|dotnet (?:\"[^\"]*MSBuild\\.dll\"|\\S*MSBuild\\.dll)) ", command.ToString());
        Assert.EndsWith("\"My App.csproj\" -verbosity:detailed -target:Clean;Compile -property:\"Configuration=Release Candidate\" -maxCpuCount:4 -restore -noLogo -nodeReuse:false", command.ToString());
    }

    [Fact]
    public void MSBuild_defaults_are_fresh_and_target_Do_Solution()
    {
        var first = Tools.MSBuild;
        var second = Tools.MSBuild;

        Assert.NotSame(first, second);
        Assert.Equal([Do.Solution.Path], first.Projects);
        Assert.Equal(MSBuildVerbosity.Normal, first.Verbosity);
    }

    [Fact]
    public void Renders_vstest_with_located_runner_and_typed_options()
    {
        var command = Tools.VSTest with
            {
                TestFiles = ["tests/My Tests.dll", "tests/Other.Tests.dll"],
                Tests = ["Product.Tests.Can ship", "Product.Tests.CanRetry"],
                Framework = ".NETCoreApp,Version=v10.0",
                Platform = VSTestPlatform.X64,
                TestEnvironment = new Dictionary<string, string> { ["DEPLOYMENT_SLOT"] = "Release Candidate" },
                Settings = "config/CI Tests.runsettings",
                Parallel = true,
                TestAdapterPath = "test adapters",
                Blame = true,
                Diag = "logs/vstest log.txt;tracelevel=info",
                Loggers = ["trx;LogFileName=CI Results.trx", "console;verbosity=detailed"],
                ResultsDirectory = "test results",
                Collect = ["Code Coverage", "XPlat Code Coverage"],
                InIsolation = true,
            };

        Assert.Equal(["tests/My Tests.dll", "tests/Other.Tests.dll"], command.TestFiles);
        Assert.Equal(["Product.Tests.Can ship", "Product.Tests.CanRetry"], command.Tests);
        Assert.Equal("Release Candidate", command.TestEnvironment["deployment_slot"]);
        Assert.Matches("^(?:\"[^\"]*vstest\\.console\\.exe\"|\\S*vstest\\.console\\.exe|dotnet (?:\"[^\"]*vstest\\.console\\.dll\"|\\S*vstest\\.console\\.dll)) ", command.ToString());
        Assert.EndsWith(
            "\"tests/My Tests.dll\" tests/Other.Tests.dll --Tests:\"Product.Tests.Can ship\",Product.Tests.CanRetry --Framework:.NETCoreApp,Version=v10.0 --Platform:x64 -e:\"DEPLOYMENT_SLOT=Release Candidate\" --Settings:\"config/CI Tests.runsettings\" --Parallel --TestAdapterPath:\"test adapters\" --Blame --Diag:\"logs/vstest log.txt;tracelevel=info\" --Logger:\"trx;LogFileName=CI Results.trx\" --Logger:console;verbosity=detailed --ResultsDirectory:\"test results\" --Collect:\"Code Coverage\" --Collect:\"XPlat Code Coverage\" --InIsolation",
            command.ToString());
    }

    [Fact]
    public void VSTest_defaults_are_fresh_and_filters_are_mutually_exclusive()
    {
        var first = Tools.VSTest;
        var second = Tools.VSTest;

        Assert.NotSame(first, second);
        Assert.Empty(first.TestFiles);
        var command = first with { Tests = ["CanShip"], TestCaseFilter = "Priority=1" };
        Assert.Throws<InvalidOperationException>(() => command.ToString());

        var alternate = second with
            {
                TestFiles = ["tests.dll"],
                TestCaseFilter = "Category=Continuous Integration",
                ListTests = true,
                TestAdapterLoadingStrategy = "Explicit",
                ParentProcessId = 123,
                Port = 456,
                AdditionalArguments = "-- custom.runSetting=true",
            };
        Assert.EndsWith(
            "tests.dll --TestCaseFilter:\"Category=Continuous Integration\" --ListTests --TestAdapterLoadingStrategy:Explicit --ParentProcessId:123 --Port:456 -- custom.runSetting=true",
            alternate.ToString());
    }

    sealed record TestToolCommand : ExecToolCommand
    {
        protected override IReadOnlyList<string?> CommandParts =>
            [
                "example",
                Arg("--value", Value),
                Args("--values", Values),
                Args("--entries", Entries.Select(pair => $"{pair.Key}={pair.Value}")),
                Arg("--raw", Raw, quote: false),
                Arg("--blank", Blank),
            ];
        public string? Value { get; init; }
        public IReadOnlyList<string> Values { get; init => field = value.ToArray(); } = [];
        public IReadOnlyDictionary<string, string> Entries { get; init => field = new Dictionary<string, string>(value).AsReadOnly(); } = ReadOnlyDictionary<string, string>.Empty;
        public string? Raw { get; init; }
        public string? Blank { get; init; }
    }

    sealed record PrefixToolCommand : ExecToolCommand
    {
        protected override IReadOnlyList<string?> CommandParts =>
            [
                "example",
                Arg("--space", "value"),
                Arg("--colon:", "value"),
                Arg("--equals=", "value"),
                Arg("-Wl,", "value"),
            ];
    }
}
