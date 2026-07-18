using System.Diagnostics;
using DotNetDo.Cli;
using Xunit;

namespace DotNetDo.Tests;

public sealed class MetaTaskTests
{
    [Fact]
    public async Task Runs_nested_tasks_in_order_and_forwards_arguments()
    {
        using var workspace = Workspace.Create(
            """
            [tasks]
            all = ["first --fixed one", "nested"]
            nested = "second --fixed two"
            """);
        workspace.WriteTask("first", "Console.WriteLine(\"first:\" + string.Join(\"|\", args));");
        workspace.WriteTask("second", "Console.WriteLine(\"second:\" + string.Join(\"|\", args));");

        var calls = new List<(string Task, string Arguments)>();
        var catalog = TaskCatalog.Load(AbsolutePath.Parse(workspace.Directory), RelativePath.Parse("scripts"));

        var exitCode = await RunCommand.RunTask(catalog, "all", "--shared \"hello world\"", (task, arguments) =>
        {
            calls.Add((task, arguments));
            return Task.FromResult(0);
        });

        Assert.Equal(0, exitCode);
        Assert.Equal(
            new[]
                {
                    ("first", "--shared \"hello world\" --fixed one"),
                    ("second", "--shared \"hello world\" --fixed two")
                },
            calls);
    }

    [Fact]
    public async Task Stops_at_the_first_failed_task()
    {
        using var workspace = Workspace.Create(
            """
            [tasks]
            all = ["fail", "second"]
            """);
        workspace.WriteTask("fail", "Console.WriteLine(\"fail\"); return 7;");
        workspace.WriteTask("second", "Console.WriteLine(\"second\");");

        var calls = new List<string>();
        var catalog = TaskCatalog.Load(AbsolutePath.Parse(workspace.Directory), RelativePath.Parse("scripts"));

        var exitCode = await RunCommand.RunTask(catalog, "all", "", (task, _) =>
        {
            calls.Add(task);
            return Task.FromResult(task == "fail" ? 7 : 0);
        });

        Assert.Equal(7, exitCode);
        Assert.Equal(new[] { "fail" }, calls);
    }

    [Fact]
    public async Task Validates_the_complete_graph_before_execution()
    {
        using var workspace = Workspace.Create(
            """
            [tasks]
            all = ["first", "missing"]
            """);
        workspace.WriteTask("first", "Console.WriteLine(\"first ran\");");

        var result = await workspace.Run("all");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("invokes unknown task 'missing'", result.Error);
        Assert.DoesNotContain("first ran", result.Output);
    }

    [Fact]
    public async Task Rejects_cycles()
    {
        using var workspace = Workspace.Create(
            """
            [tasks]
            first = "second"
            second = "first"
            """);

        var result = await workspace.Run("first");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Meta-task cycle: first -> second -> first", result.Error);
    }

    [Fact]
    public async Task Rejects_task_name_collisions()
    {
        using var workspace = Workspace.Create(
            """
            [tasks]
            build = "leaf"
            """);
        workspace.WriteTask("build", "Console.WriteLine(\"build\");");
        workspace.WriteTask("leaf", "Console.WriteLine(\"leaf\");");

        var result = await workspace.Run();

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("defined by both", result.Error);
    }

    [Fact]
    public async Task Lists_and_describes_meta_tasks()
    {
        using var workspace = Workspace.Create(
            """
            [tasks]
            test = ["build --configuration Release", "test-csharp"]
            """);
        workspace.WriteTask("build", "Console.WriteLine(\"build\");");
        workspace.WriteTask("test-csharp", "Console.WriteLine(\"test\");");

        var list = await workspace.Run();
        var help = await workspace.Run(":help", "test");

        Assert.Equal(0, list.ExitCode);
        Assert.Equal(new[] { "build", "test", "test-csharp" }, list.OutputLines);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Invocations:", help.Output);
        Assert.Contains("  build --configuration Release", help.Output);
        Assert.Contains("  test-csharp", help.Output);
        Assert.Contains("Arguments are forwarded to each task.", help.Output);
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

        public static Workspace Create(string configuration)
        {
            var directory = Path.Combine(Path.GetTempPath(), $"dotnetdo-meta-{Guid.NewGuid():N}");
            System.IO.Directory.CreateDirectory(directory);
            return new(directory, configuration);
        }

        public void WriteTask(string name, string source) =>
            File.WriteAllText(Path.Combine(Directory, "scripts", $"{name}.cs"), source);

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

    sealed record Result(int ExitCode, string Output, string Error)
    {
        public string[] OutputLines => Output
            .ReplaceLineEndings("\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }
}
