using System.Diagnostics;
using System.Text.Json;
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

        var result = await workspace.Run("all", "--shared", "hello world", "--", "tool", "--flag");

        Assert.True(result.ExitCode == 0, result.Output + result.Error);
        Assert.Equal(
            ["first:--shared|hello world|--fixed|one|--|tool|--flag",
             "second:--shared|hello world|--fixed|two|--|tool|--flag"],
            result.OutputLines);
    }

    [Fact]
    public void Separates_trailing_arguments_before_rendering()
    {
        var commandLine = TaskCommandLine.FromArguments(["--configuration", "Debug", "--", "interactive", "--config", "my file"])
            .AppendParameters("--configuration", "Release");

        Assert.Equal(["--configuration", "Debug", "--configuration", "Release"], commandLine.Parameters);
        Assert.Equal(["interactive", "--config", "my file"], commandLine.Arguments);
        Assert.Equal("--configuration Debug --configuration Release -- interactive --config \"my file\"", commandLine.Render());
    }

    [Theory]
    [InlineData("yes", "true")]
    [InlineData("N", "false")]
    [InlineData("false", "false")]
    public void Preflight_normalizes_boolean_shortcuts(string input, string expected)
    {
        var parameter = new TaskHelp.TaskParameter("publish", "bool", null, null, true, false, ["true", "false"]);

        Assert.True(ParameterPrompt.TryNormalize(input, parameter.Type, parameter.Values, out var value));
        Assert.Equal(expected, value);
    }

    [Fact]
    public void Parses_configured_arguments_once()
    {
        var commandLine = TaskCommandLine.ParseConfigured("--name \"hello world\" --publish=true -- tool \"some file\"");

        Assert.Equal(["--name", "hello world", "--publish=true"], commandLine.Parameters);
        Assert.Equal(["tool", "some file"], commandLine.Arguments);
    }

    [Theory]
    [InlineData("\"say \\\"hello\\\"\"", "say \"hello\"")]
    [InlineData("\"C:\\some folder\\\\\"", "C:\\some folder\\")]
    [InlineData("\"\"", "")]
    [InlineData("\"a\"\"b\"", "a\"b")]
    [InlineData("one\" two \"three", "one two three")]
    [InlineData("C:\\folder\\", "C:\\folder\\")]
    [InlineData("a\\\\\"\"", "a\\")]
    [InlineData("\"a\\\\\\\"b\"", "a\\\"b")]
    public void Preserves_configured_argument_escaping(string configured, string expected)
    {
        var commandLine = TaskCommandLine.ParseConfigured($"--value {configured} -- {configured}");

        Assert.Equal(["--value", expected], commandLine.Parameters);
        Assert.Equal([expected], commandLine.Arguments);
    }

    [Fact]
    public async Task Preserves_configured_and_inherited_values_in_the_child_process()
    {
        using var workspace = Workspace.Create(
            """
            [tasks]
            all = 'echo --message "say \"hello\"" --path "C:\some folder\\" -- tool "" "fixed \"quote\""'
            """);
        workspace.WriteTask("echo", "Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(args));");

        var result = await workspace.Run("all", "--", "", "inherited \"quote\"", "C:\\another folder\\", "--");

        Assert.True(result.ExitCode == 0, result.Output + result.Error);
        Assert.Equal(
            ["--message", "say \"hello\"", "--path", "C:\\some folder\\", "--",
             "", "inherited \"quote\"", "C:\\another folder\\", "--", "tool", "", "fixed \"quote\""],
            JsonSerializer.Deserialize<string[]>(Assert.Single(result.OutputLines))!);
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

        var result = await workspace.Run("all");

        Assert.Equal(7, result.ExitCode);
        Assert.Equal(["fail"], result.OutputLines);
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
        workspace.WriteTask("build", """
            [assembly: DotNetDo.TaskDescription("Build the solution")]
            Console.WriteLine("build");
            """);
        workspace.WriteTask("test-csharp", "Console.WriteLine(\"test\");");

        var list = await workspace.Run();
        var help = await workspace.Run(":help", "test");
        var taskHelp = await workspace.Run(":help", "build");

        Assert.Equal(0, list.ExitCode);
        Assert.Equal(
            [
                "Usage: ./do <task> [args...]",
                "Tasks:",
                "  build        Build the solution",
                "  test",
                "  test-csharp"
            ],
            list.OutputLines);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("Invocations:", help.Output);
        Assert.Contains("  build --configuration Release", help.Output);
        Assert.Contains("  test-csharp", help.Output);
        Assert.Contains("Arguments are forwarded to each task.", help.Output);
        Assert.Contains("Build the solution", taskHelp.Output);
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
            File.WriteAllText(Path.Combine(Directory, "scripts", $"{name}.cs"), "#:property PublishAot=false\n" + source);

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
            var output = process.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
            var error = process.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
            await process.WaitForExitAsync(TestContext.Current.CancellationToken);
            return new(process.ExitCode, await output, await error);
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
