using System.Diagnostics;
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

        var result = await workspace.Run(":new", "build");

        Assert.Equal(0, result.ExitCode);
        Assert.True(workspace.TaskExists("build"));
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

        var result = await workspace.Run(":new", "build");

        Assert.NotEqual(0, result.ExitCode);
        Assert.False(workspace.TaskExists("build"));
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

        public async Task<Result> Run(params string[] arguments)
        {
            var startInfo = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = Directory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "DotNetDo.dll"));
            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using var process = Process.Start(startInfo)!;
            var output = await process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
            var error = await process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
            await process.WaitForExitAsync(TestContext.Current.CancellationToken);
            return new(process.ExitCode, output, error);
        }

        public void Dispose() => System.IO.Directory.Delete(Directory, recursive: true);
    }

    sealed record Result(int ExitCode, string Output, string Error);
}
