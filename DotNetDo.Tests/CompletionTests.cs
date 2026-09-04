using System.Diagnostics;
using DotNetDo.Cli;
using Xunit;

namespace DotNetDo.Tests;

public sealed class CompletionTests
{
    [Fact]
    public void Completes_tool_commands_and_tasks_by_case_insensitive_prefix()
    {
        using var workspace = Workspace.Create();
        workspace.WriteTask("Build", """[assembly: DotNetDo.TaskDescription("Build the solution")]""");
        var catalog = workspace.Catalog();

        var candidates = Complete(catalog, workspace.Root, 1, "dotnetdo", "b");

        Assert.Equal(["Build"], candidates.Select(candidate => candidate.Value));
        Assert.Equal("Build the solution", candidates[0].Detail);
        var commands = Complete(catalog, workspace.Root, 1, "dotnetdo", ":").Select(candidate => candidate.Value);
        Assert.Equal(
            CliCommands.Visible.Where(command => command != CliCommands.Completion).Select(command => command.Name).Order(StringComparer.OrdinalIgnoreCase)
                .Append(CliCommands.Completion.Name),
            commands);
    }

    [Fact]
    public void Prioritizes_tasks_over_management_commands()
    {
        using var workspace = Workspace.Create();
        workspace.WriteTask("release", "");
        var catalog = workspace.Catalog();

        var candidates = Complete(catalog, workspace.Root, 1, "dotnetdo", "");

        Assert.Equal("release", candidates[0].Value);
        Assert.Equal(CliCommands.Completion.Name, candidates[^1].Value);
    }

    [Fact]
    public void Completes_file_tasks_for_rename()
    {
        using var workspace = Workspace.Create("[tasks]\nall = \"build\"");
        workspace.WriteTask("build", "");
        var catalog = workspace.Catalog();

        var candidates = Complete(catalog, workspace.Root, 2, "dotnetdo", ":rename", "");

        Assert.Equal(["build"], candidates.Select(candidate => candidate.Value));
    }

    [Fact]
    public void Completes_parameter_names_boolean_values_and_local_enum_members()
    {
        using var workspace = Workspace.Create();
        workspace.WriteTask(
            "build",
            """
            enum Mode
            {
                Fast,
                [Obsolete] Safe = 4,
                Custom = Value("ignored, text"),
                Combined = Combine(Fast, Safe),
                Last
            }

            var mode = Do.Param<Mode>("mode", Mode.Fast, "Execution mode");
            var pack = Do.Param<bool>("pack", false, "Create package");
            """);
        var catalog = workspace.Catalog();

        var names = Complete(catalog, workspace.Root, 2, "dotnetdo", "build", "--");
        Assert.Equal(["--mode", "--pack"], names.Select(candidate => candidate.Value));
        Assert.Contains("Mode", names[0].Detail);
        Assert.Contains("Execution mode", names[0].Detail);

        Assert.Equal(
            ["true"],
            Complete(catalog, workspace.Root, 3, "dotnetdo", "build", "--pack", "t").Select(candidate => candidate.Value));
        Assert.Equal(
            ["--mode=Safe"],
            Complete(catalog, workspace.Root, 2, "dotnetdo", "build", "--mode=S").Select(candidate => candidate.Value));
        Assert.Equal(
            ["--mode=Combined", "--mode=Custom", "--mode=Fast", "--mode=Last", "--mode=Safe"],
            Complete(catalog, workspace.Root, 2, "dotnetdo", "build", "--mode=").Select(candidate => candidate.Value));
    }

    [Fact]
    public void Suppresses_used_parameters_except_the_option_being_edited()
    {
        using var workspace = Workspace.Create();
        workspace.WriteTask(
            "build",
            """
            var first = Do.Param("first");
            var second = Do.Param("second");
            """);
        var catalog = workspace.Catalog();

        var candidates = Complete(catalog, workspace.Root, 4, "dotnetdo", "build", "--first", "value", "--");

        Assert.Equal(["--second"], candidates.Select(candidate => candidate.Value));
    }

    [Fact]
    public void Recursively_unions_meta_task_parameters_and_drops_conflicting_detail()
    {
        using var workspace = Workspace.Create(
            """
            [tasks]
            all = ["first", "nested"]
            nested = "second --shared fixed"
            """);
        workspace.WriteTask(
            "first",
            """
            var shared = Do.Param<bool>("shared", false, "First meaning");
            var first = Do.Param("first");
            """);
        workspace.WriteTask(
            "second",
            """
            var shared = Do.Param<int>("shared", 1, "Second meaning");
            var second = Do.Param("second");
            """);
        var catalog = workspace.Catalog();

        var candidates = Complete(catalog, workspace.Root, 2, "dotnetdo", "all", "--");

        Assert.Equal(["--first", "--second", "--shared"], candidates.Select(candidate => candidate.Value));
        Assert.Null(candidates.Single(candidate => candidate.Value == "--shared").Detail);
    }

    [Fact]
    public void Completes_builtin_arguments()
    {
        using var workspace = Workspace.Create();
        workspace.WriteTask("build", "#:package Example.Package@1.0.0");
        var catalog = workspace.Catalog();

        Assert.Equal(
            ["build"],
            Complete(catalog, workspace.Root, 2, "dotnetdo", ":help", "b").Select(candidate => candidate.Value));
        Assert.Equal(
            ["--all", "--prerelease", "Example.Package"],
            Complete(catalog, workspace.Root, 2, "dotnetdo", ":update", "").Select(candidate => candidate.Value));
        Assert.Equal(
            ["bash"],
            Complete(catalog, workspace.Root, 2, "dotnetdo", ":completion", "b").Select(candidate => candidate.Value));
        Assert.Equal(
            ["zsh"],
            Complete(catalog, workspace.Root, 3, "dotnetdo", ":completion", "uninstall", "z").Select(candidate => candidate.Value));
    }

    [Fact]
    public void Hidden_protocol_ignores_invalid_input()
    {
        using var output = new StringWriter();

        Assert.Equal(0, CompleteCommand.Run([":complete", "invalid"], output));
        Assert.Equal("", output.ToString());
    }

    [Theory]
    [InlineData(1, new[] { "dotnetdo" }, new[] { "" })]
    [InlineData(3, new[] { "dotnetdo", "release", "--" }, new[] { "release", "--", "" })]
    public void Hidden_protocol_restores_an_empty_active_token_dropped_by_the_shell(
        int activeTokenIndex,
        string[] shellTokens,
        string[] expectedArguments)
    {
        var args = new[] { ":complete", activeTokenIndex.ToString(), "--" }.Concat(shellTokens).ToArray();

        Assert.True(CompleteCommand.CompletionRequest.TryParse(args, out var request));
        Assert.Equal(expectedArguments, request.Arguments);
        Assert.Equal(expectedArguments.Length - 1, request.ActiveArgumentIndex);
    }

    [Theory]
    [InlineData("pwsh", "Documents/PowerShell/Microsoft.PowerShell_profile.ps1", "dotnetdo-completion.ps1")]
    [InlineData("bash", ".bashrc", "dotnetdo-completion.bash")]
    [InlineData("zsh", ".zshrc", "dotnetdo-completion.zsh")]
    public void Installs_updates_and_uninstalls_owned_profile_content(string shell, string relativeProfile, string adapterName)
    {
        using var workspace = Workspace.Create();
        var profile = Path.Combine(workspace.Directory, relativeProfile.Replace('/', Path.DirectorySeparatorChar));
        var root = AbsolutePath.Parse(workspace.Directory);
        var data = root / "data";
        Directory.CreateDirectory(Path.GetDirectoryName(profile)!);
        File.WriteAllText(profile, "existing");

        Assert.Equal(0, CompletionCommand.Run(false, shell, root, data, true));
        Assert.Equal(0, CompletionCommand.Run(false, shell, root, data, true));

        var installed = File.ReadAllText(profile);
        Assert.StartsWith("existing", installed);
        Assert.Equal(1, Count(installed, "# >>> DotNetDo completion >>>"));
        Assert.True((data / adapterName).IsExistingFile);

        Assert.Equal(0, CompletionCommand.Run(true, shell, root, data, false));
        Assert.Equal("existing" + Environment.NewLine + Environment.NewLine, File.ReadAllText(profile));
        Assert.False((data / adapterName).IsExistingFile);
    }

    [Fact]
    public void Refuses_to_install_completion_without_dotnetdo_on_path()
    {
        using var workspace = Workspace.Create();
        var root = AbsolutePath.Parse(workspace.Directory);
        var data = root / "data";
        using var error = new StringWriter();

        var result = CompletionCommand.Run(
            false,
            "pwsh",
            root,
            data,
            completionHostAvailable: false,
            error: error);

        Assert.Equal(1, result);
        Assert.Equal(
            "Shell completion requires dotnetdo on PATH. Install DotNetDo globally, then run dotnetdo :completion." + Environment.NewLine,
            error.ToString());
        Assert.False((data / "dotnetdo-completion.ps1").Exists);
    }

    [Fact]
    public void PowerShell_adapter_uses_the_installed_tool_to_complete_the_local_do_launcher()
    {
        using var workspace = Workspace.Create();
        var root = AbsolutePath.Parse(workspace.Directory);
        var data = root / "data";

        Assert.Equal(0, CompletionCommand.Run(false, "pwsh", root, data, true));

        var adapter = (data / "dotnetdo-completion.ps1").ReadText();
        Assert.Contains("-CommandName dotnetdo, do", adapter);
        Assert.Contains("& dotnetdo :complete", adapter);
        Assert.DoesNotContain("& $tokens[0] :complete", adapter);
    }

    [Fact]
    public void PowerShell_adapter_does_not_execute_the_local_launcher_during_completion()
    {
        if (!OperatingSystem.IsWindows())
            return;

        using var workspace = Workspace.Create();
        var root = AbsolutePath.Parse(workspace.Directory);
        var data = root / "data";
        var commands = root / "commands";
        commands.EnsureDirectoryExists();
        (commands / "dotnetdo.cmd").WriteText("@echo build\tTask\r\n");
        (root / "do.cmd").WriteText("@echo local launcher must not run\r\n");
        Assert.Equal(0, CompletionCommand.Run(false, "pwsh", root, data, true));

        var adapter = data / "dotnetdo-completion.ps1";
        var source = adapter.ToString().Replace("'", "''");
        var startInfo = new ProcessStartInfo("pwsh")
        {
            WorkingDirectory = workspace.Directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add($". '{source}'; [System.Management.Automation.CommandCompletion]::CompleteInput('.\\do b', 6, $null).CompletionMatches.CompletionText");
        startInfo.Environment["PATH"] = $"{commands}{Path.PathSeparator}{startInfo.Environment["PATH"]}";

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.True(process.ExitCode == 0, error);
        Assert.Equal("", error);
        Assert.Equal("build", output.Trim());
    }

    [Theory]
    [InlineData("bash", "dotnetdo-completion.bash", "dotnetdo :complete", "\"${COMP_WORDS[0]}\" :complete", "dotnetdo do")]
    [InlineData("zsh", "dotnetdo-completion.zsh", "dotnetdo :complete", "\"${words[1]}\" :complete", "dotnetdo do")]
    public void Unix_adapter_uses_the_installed_tool_to_complete_the_local_do_launcher(
        string shell,
        string adapterName,
        string invocation,
        string launcherInvocation,
        string registrations)
    {
        using var workspace = Workspace.Create();
        var root = AbsolutePath.Parse(workspace.Directory);
        var data = root / "data";

        Assert.Equal(0, CompletionCommand.Run(false, shell, root, data, true));

        var adapter = (data / adapterName).ReadText();
        Assert.Contains(invocation, adapter);
        Assert.DoesNotContain(launcherInvocation, adapter);
        Assert.Contains(registrations, adapter);
    }

    [Theory]
    [InlineData("# >>> DotNetDo completion >>>")]
    [InlineData("# <<< DotNetDo completion <<<")]
    [InlineData("# >>> DotNetDo completion >>>\nowned\n# <<< DotNetDo completion <<<\n# >>> DotNetDo completion >>>")]
    public void Refuses_to_change_profiles_with_unmatched_or_duplicate_markers(string existing)
    {
        using var workspace = Workspace.Create();
        var profile = Path.Combine(workspace.Directory, ".bashrc");
        var root = AbsolutePath.Parse(workspace.Directory);
        var data = root / "data";
        File.WriteAllText(profile, existing);

        Assert.Throws<InvalidDataException>(() =>
            CompletionCommand.Run(false, "bash", root, data, true));
        Assert.Equal(existing, File.ReadAllText(profile));
    }

    static CompletionCandidate[] Complete(TaskCatalog catalog, AbsolutePath root, int activeIndex, params string[] tokens) =>
        CompletionEngine.Complete(catalog, root, tokens[1..], activeIndex - 1);

    static int Count(string value, string text) =>
        value.Split(text, StringSplitOptions.None).Length - 1;

    sealed class Workspace : IDisposable
    {
        Workspace(string directory, string configuration)
        {
            Directory = directory;
            DirectoryInfo = AbsolutePath.Parse(directory);
            System.IO.Directory.CreateDirectory(Path.Combine(directory, "scripts"));
            File.WriteAllText(Path.Combine(directory, "dotnetdo.toml"), configuration);
        }

        public string Directory { get; }
        public AbsolutePath Root => DirectoryInfo;
        AbsolutePath DirectoryInfo { get; }

        public static Workspace Create(string configuration = "") =>
            new(Path.Combine(Path.GetTempPath(), $"dotnetdo-completion-{Guid.NewGuid():N}"), configuration);

        public TaskCatalog Catalog() => TaskCatalog.Load(Root, RelativePath.Parse("scripts"));

        public void WriteTask(string name, string source) =>
            File.WriteAllText(Path.Combine(Directory, "scripts", $"{name}.cs"), source);

        public void Dispose() => System.IO.Directory.Delete(Directory, recursive: true);
    }
}
