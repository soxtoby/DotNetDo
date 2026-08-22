using System.Diagnostics;
using Xunit;

namespace DotNetDo.Tests;

public sealed class RenameCommandTests
{
    [Fact]
    public async Task Renames_task_file_without_changing_contents()
    {
        using var workspace = Workspace.Create();
        workspace.WriteTask("build", "Console.WriteLine(\"build\");");

        var result = await workspace.Run(":rename", "build", "compile");

        Assert.Equal(0, result.ExitCode);
        Assert.False(workspace.TaskExists("build"));
        Assert.Equal("Console.WriteLine(\"build\");", workspace.ReadTask("compile"));
        Assert.Contains("Renamed scripts/build.cs to scripts/compile.cs", result.Output.Replace('\\', '/'));
    }

    [Fact]
    public async Task Supports_case_only_renames()
    {
        using var workspace = Workspace.Create();
        workspace.WriteTask("build", "source");

        var result = await workspace.Run(":rename", "build", "Build");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            "Build.cs",
            Directory.GetFiles(Path.Combine(workspace.Directory, "scripts")).Select(Path.GetFileName).Single());
        Assert.Equal("source", workspace.ReadTask("Build"));
    }

    [Fact]
    public async Task Synchronizes_configured_solution_folder()
    {
        using var workspace = Workspace.Create(
            "scripts-path = \"scripts\"\nsolution-path = \"Product.slnx\"\nsolution-folder = \"Tasks\"\n");
        workspace.WriteTask("build", "");
        workspace.Write(
            "Product.slnx",
            """
            <Solution>
              <Folder Name="/Tasks/">
                <File Path="README.md" />
                <File Path="scripts/build.cs" />
              </Folder>
            </Solution>
            """);

        var result = await workspace.Run(":rename", "build", "compile");

        Assert.Equal(0, result.ExitCode);
        var solution = workspace.Read("Product.slnx");
        Assert.Contains("scripts/compile.cs", solution);
        Assert.DoesNotContain("scripts/build.cs", solution);
        Assert.Contains("README.md", solution);
    }

    [Fact]
    public async Task Leaves_solution_without_configured_folder_unchanged()
    {
        using var workspace = Workspace.Create("solution-path = \"Product.slnx\"\n");
        workspace.WriteTask("build", "");
        workspace.Write("Product.slnx", "<Solution><File Path=\"scripts/build.cs\" /></Solution>");

        var result = await workspace.Run(":rename", "build", "compile");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("<Solution><File Path=\"scripts/build.cs\" /></Solution>", workspace.Read("Product.slnx"));
    }

    [Fact]
    public async Task Requires_old_and_new_names()
    {
        using var workspace = Workspace.Create();

        foreach (var arguments in new[]
                 {
                     new[] { ":rename" },
                     new[] { ":rename", "build" },
                     new[] { ":rename", "build", "compile", "extra" }
                 })
        {
            var result = await workspace.Run(arguments);

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Usage: dotnet do :rename <old-name> <new-name>", result.Error);
        }
    }

    [Theory]
    [InlineData("missing", "compile", "scripts/missing.cs does not exist.")]
    [InlineData("build.cs", "compile", TaskName.InvalidMessage)]
    [InlineData("build", "nested/compile", TaskName.InvalidMessage)]
    public async Task Rejects_invalid_or_missing_tasks(string oldName, string newName, string error)
    {
        using var workspace = Workspace.Create();
        workspace.WriteTask("build", "source");

        var result = await workspace.Run(":rename", oldName, newName);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(error, result.Error.Replace('\\', '/'));
        Assert.Equal("source", workspace.ReadTask("build"));
    }

    [Fact]
    public async Task Refuses_to_replace_an_existing_task()
    {
        using var workspace = Workspace.Create();
        workspace.WriteTask("build", "build");
        workspace.WriteTask("compile", "compile");

        var result = await workspace.Run(":rename", "build", "compile");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("scripts/compile.cs already exists.", result.Error.Replace('\\', '/'));
        Assert.Equal("build", workspace.ReadTask("build"));
        Assert.Equal("compile", workspace.ReadTask("compile"));
    }

    [Fact]
    public async Task Refuses_to_collide_with_a_meta_task()
    {
        using var workspace = Workspace.Create("[tasks]\ncompile = \"build\"\n");
        workspace.WriteTask("build", "build");

        var result = await workspace.Run(":rename", "build", "compile");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Task 'compile' is already defined in 'dotnetdo.toml'.", result.Error);
        Assert.True(workspace.TaskExists("build"));
    }

    sealed class Workspace : IDisposable
    {
        Workspace(string directory, string configuration)
        {
            Directory = directory;
            System.IO.Directory.CreateDirectory(Path.Combine(directory, "scripts"));
            File.WriteAllText(Path.Combine(directory, "dotnetdo.toml"), configuration);
        }

        public string Directory { get; }

        public static Workspace Create(string configuration = "")
        {
            var directory = Path.Combine(Path.GetTempPath(), $"dotnetdo-rename-{Guid.NewGuid():N}");
            System.IO.Directory.CreateDirectory(directory);
            return new(directory, configuration);
        }

        public void WriteTask(string name, string source) => Write($"scripts/{name}.cs", source);
        public bool TaskExists(string name) => File.Exists(Path.Combine(Directory, "scripts", $"{name}.cs"));
        public string ReadTask(string name) => Read($"scripts/{name}.cs");
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
