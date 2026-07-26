using DotNetDo.Cli;
using NuGet.Versioning;
using Xunit;

namespace DotNetDo.Tests;

public sealed class UpdateCommandTests
{
    [Fact]
    public async Task Default_updates_manifest_DotNetDo_and_Core_pins()
    {
        using var workspace = Workspace.Create(
            """
            #:package DotNetDo.Core@1.0.0
            #:package Serilog@2.0.0
            """,
            manifest: true);
        var client = new Client(("DotNetDo.Core", "1.1.0"));

        var result = await UpdateCommand.Run([":update"], workspace.Root, workspace.Scripts, client);

        Assert.Equal(0, result);
        Assert.Equal(["DotNetDo.Core"], client.Searches);
        Assert.Equal(["DotNetDo"], client.ToolUpdates);
        Assert.Contains("DotNetDo.Core@1.1.0", workspace.ScriptText);
        Assert.Contains("Serilog@2.0.0", workspace.ScriptText);
    }

    [Fact]
    public async Task Named_package_updates_only_matching_pins_case_insensitively()
    {
        using var workspace = Workspace.Create(
            """
            #:package Serilog@2.0.0
            #:package serilog@2.1.0
            #:package DotNetDo.Core@1.0.0
            """,
            manifest: true);
        var client = new Client(("Serilog", "3.0.0"));

        var result = await UpdateCommand.Run([":update", "serilog"], workspace.Root, workspace.Scripts, client);

        Assert.Equal(0, result);
        Assert.Empty(client.ToolUpdates);
        Assert.Contains("Serilog@3.0.0", workspace.ScriptText);
        Assert.Contains("serilog@3.0.0", workspace.ScriptText);
        Assert.Contains("DotNetDo.Core@1.0.0", workspace.ScriptText);
    }

    [Fact]
    public async Task All_updates_every_exact_pin_and_the_manifest()
    {
        using var workspace = Workspace.Create(
            """
            #:package DotNetDo.Core@1.0.0
            #:package Serilog@2.0.0
            #:package Floating@1.*
            """,
            manifest: true);
        var client = new Client(("DotNetDo.Core", "1.1.0"), ("Serilog", "3.0.0"));

        var result = await UpdateCommand.Run([":update", "--all", "--prerelease"], workspace.Root, workspace.Scripts, client);

        Assert.Equal(0, result);
        Assert.Equal(["DotNetDo.Core", "Serilog"], client.Searches);
        Assert.All(client.PrereleaseSearches, Assert.True);
        Assert.Equal(["DotNetDo"], client.ToolUpdates);
        Assert.True(client.ToolPrerelease);
        Assert.Contains("Floating@1.*", workspace.ScriptText);
    }

    [Fact]
    public async Task Older_candidates_never_downgrade_pins()
    {
        using var workspace = Workspace.Create("#:package Example@2.0.0-beta.2");
        var client = new Client(("Example", "1.9.0"));

        var result = await UpdateCommand.Run([":update", "Example"], workspace.Root, workspace.Scripts, client);

        Assert.Equal(0, result);
        Assert.Contains("Example@2.0.0-beta.2", workspace.ScriptText);
    }

    [Fact]
    public async Task Missing_named_pin_fails_without_external_calls()
    {
        using var workspace = Workspace.Create("#:package Other@1.0.0", manifest: true);
        var client = new Client();

        var result = await UpdateCommand.Run([":update", "Missing"], workspace.Root, workspace.Scripts, client);

        Assert.Equal(1, result);
        Assert.Empty(client.Searches);
        Assert.Empty(client.ToolUpdates);
    }

    [Theory]
    [InlineData(":update", "--all", "Example")]
    [InlineData(":update", "--unknown")]
    [InlineData(":update", "--all", "--all")]
    [InlineData(":update", "One", "Two")]
    public async Task Invalid_arguments_fail(params string[] args)
    {
        using var workspace = Workspace.Create("");
        var client = new Client();

        var result = await UpdateCommand.Run(args, workspace.Root, workspace.Scripts, client);

        Assert.Equal(1, result);
        Assert.Empty(client.Searches);
    }

    sealed class Client(params (string Package, string Version)[] versions) : IUpdateClient
    {
        readonly Dictionary<string, NuGetVersion> _versions = versions.ToDictionary(
            item => item.Package,
            item => NuGetVersion.Parse(item.Version),
            StringComparer.OrdinalIgnoreCase);

        public List<string> Searches { get; } = [];
        public List<bool> PrereleaseSearches { get; } = [];
        public List<string> ToolUpdates { get; } = [];
        public bool ToolPrerelease { get; private set; }

        public Task<NuGetVersion> FindLatest(string package, bool prerelease, AbsolutePath root)
        {
            Searches.Add(package);
            PrereleaseSearches.Add(prerelease);
            return Task.FromResult(_versions[package]);
        }

        public Task<ToolChange?> UpdateTool(string package, AbsolutePath manifest, bool prerelease, AbsolutePath root)
        {
            ToolUpdates.Add(package);
            ToolPrerelease = prerelease;
            return Task.FromResult<ToolChange?>(null);
        }
    }

    sealed class Workspace : IDisposable
    {
        Workspace(string directory)
        {
            Root = AbsolutePath.Parse(directory);
            Scripts = Root / "scripts";
        }

        public AbsolutePath Root { get; }
        public AbsolutePath Scripts { get; }
        AbsolutePath Script => Scripts / "build.cs";
        public string ScriptText => File.ReadAllText(Script);

        public static Workspace Create(string script, bool manifest = false)
        {
            var workspace = new Workspace(Path.Combine(Path.GetTempPath(), $"dotnetdo-update-{Guid.NewGuid():N}"));
            Directory.CreateDirectory(workspace.Scripts);
            File.WriteAllText(workspace.Script, script);
            if (manifest)
            {
                Directory.CreateDirectory(workspace.Root / ".config");
                File.WriteAllText(
                    workspace.Root / ".config/dotnet-tools.json",
                    """{"version":1,"isRoot":true,"tools":{"dotnetdo":{"version":"1.0.0","commands":["dotnet-do"]}}}""");
            }

            return workspace;
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
