using DotNetDo.Cli;
using NuGet.Versioning;
using Xunit;

namespace DotNetDo.Tests;

public sealed class NewCommandTests
{
    [Fact]
    public async Task Creates_task_and_synchronizes_configured_solution_folder()
    {
        using var workspace = Workspace.Create(
            "scripts-path = \"scripts\"\nsolution-path = \"Product.slnx\"\nsolution-folder = \"Tasks\"\n");
        workspace.Write(
            "Product.slnx",
            """
            <Solution>
              <Folder Name="/Tasks/">
                <File Path="README.md" />
              </Folder>
            </Solution>
            """);

        var client = new Client();

        var result = await workspace.Run(client, ":new", "build");

        Assert.Equal(0, result);
        Assert.True(workspace.TaskExists("build"));
        Assert.Contains("#:package DotNetDo.Core@9.8.7", workspace.Read("scripts/build.cs"));
        Assert.Equal(["DotNetDo.Core"], client.Searches);
        Assert.All(client.PrereleaseSearches, Assert.False);
        var solution = workspace.Read("Product.slnx");
        Assert.Contains("scripts/build.cs", solution);
        Assert.Contains("README.md", solution);
    }

    [Fact]
    public async Task Removes_created_task_when_solution_synchronization_fails()
    {
        using var workspace = Workspace.Create(
            "scripts-path = \"scripts\"\nsolution-path = \"Product.slnx\"\nsolution-folder = \"Tasks\"\n");
        workspace.Write("Product.slnx", "not XML");

        await Assert.ThrowsAnyAsync<Exception>(() => workspace.Run(new Client(), ":new", "build"));

        Assert.False(workspace.TaskExists("build"));
    }

    [Fact]
    public async Task Package_search_failure_leaves_task_uncreated()
    {
        using var workspace = Workspace.Create("scripts-path = \"scripts\"\n");

        var result = await workspace.Run(new Client(error: "Package lookup failed."), ":new", "build");

        Assert.Equal(1, result);
        Assert.False(workspace.TaskExists("build"));
    }

    sealed class Client(string version = "9.8.7", string? error = null) : IPackageVersionResolver
    {
        public List<string> Searches { get; } = [];
        public List<bool> PrereleaseSearches { get; } = [];

        public Task<NuGetVersion> FindLatest(string package, bool prerelease, AbsolutePath root)
        {
            Searches.Add(package);
            PrereleaseSearches.Add(prerelease);
            return error is null
                ? Task.FromResult(NuGetVersion.Parse(version))
                : Task.FromException<NuGetVersion>(new PackageLookupException(error));
        }
    }

    sealed class Workspace : IDisposable
    {
        Workspace(string directory, string configuration)
        {
            Directory = directory;
            System.IO.Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "dotnetdo.toml"), configuration);
        }

        public string Directory { get; }
        public AbsolutePath Root => AbsolutePath.Parse(Directory);

        public static Workspace Create(string configuration)
        {
            var directory = Path.Combine(Path.GetTempPath(), $"dotnetdo-new-{Guid.NewGuid():N}");
            return new(directory, configuration);
        }

        public bool TaskExists(string name) => File.Exists(Path.Combine(Directory, "scripts", $"{name}.cs"));
        public string Read(string path) => File.ReadAllText(Path.Combine(Directory, path));

        public void Write(string path, string contents)
        {
            var fullPath = Path.Combine(Directory, path);
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, contents);
        }

        public Task<int> Run(IPackageVersionResolver packageVersions, params string[] arguments) =>
            NewCommand.Run(arguments, Root, packageVersions);

        public void Dispose() => System.IO.Directory.Delete(Directory, recursive: true);
    }
}
