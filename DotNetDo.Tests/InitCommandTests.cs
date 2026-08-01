using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace DotNetDo.Tests;

public sealed class InitCommandTests
{
    [Fact]
    public async Task Initializes_workspace_with_defaults()
    {
        using var workspace = Workspace.Create();

        var result = await RunInit(workspace.Directory, "\n\n\n");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Scripts path (default: scripts):", result.Output);
        Assert.Contains("Initial task name (default: build):", result.Output);
        Assert.Equal("scripts-path = \"scripts\"\n", File.ReadAllText(Path.Combine(workspace.Directory, "dotnetdo.toml")).ReplaceLineEndings("\n"));
        var task = File.ReadAllText(Path.Combine(workspace.Directory, "scripts", "build.cs"));
        var version = typeof(Cli.TaskScaffolding).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion
            .Split('+', 2)[0];
        Assert.Contains($"#:package DotNetDo.Core@{version}", task);
        Assert.Contains("""[assembly: TaskDescription("Says hello")]""", task);
        Assert.Contains("""Log.Information("Hello from {Task}", "build");""", task);
        Assert.Equal("@dnx DotNetDo %*\r\n", File.ReadAllText(Path.Combine(workspace.Directory, "do.cmd")));
        Assert.Equal("#!/usr/bin/env sh\nexec dnx DotNetDo \"$@\"\n", File.ReadAllText(Path.Combine(workspace.Directory, "do")));
        Assert.Contains("Created scripts", result.Output);
        Assert.Contains("Created do.cmd launcher", result.Output);
        Assert.Contains("Created do launcher", result.Output);
        if (OperatingSystem.IsWindows())
            Assert.Contains("Before committing, run: git add --chmod=+x do", result.Output);
        else
            Assert.DoesNotContain("git add --chmod=+x do", result.Output);
        Assert.Contains(OperatingSystem.IsWindows() ? @"Run with: .\do build" : "Run with: ./do build", result.Output);
    }

    [Fact]
    public async Task Records_the_only_solution_recursively()
    {
        using var workspace = Workspace.Create();
        Directory.CreateDirectory(Path.Combine(workspace.Directory, "src"));
        File.WriteAllText(Path.Combine(workspace.Directory, "src", "Product.slnx"), "<Solution />");

        var result = await RunInit(workspace.Directory, "\n\n\n");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            "scripts-path = \"scripts\"\nsolution-path = \"src/Product.slnx\"\nsolution-folder = \"scripts\"\n",
            File.ReadAllText(Path.Combine(workspace.Directory, "dotnetdo.toml")).ReplaceLineEndings("\n"));
        Assert.Contains("../scripts/build.cs", File.ReadAllText(Path.Combine(workspace.Directory, "src", "Product.slnx")));
    }

    [Fact]
    public async Task Requires_selection_from_solutions_ordered_by_depth_then_name()
    {
        using var workspace = Workspace.Create();
        Directory.CreateDirectory(Path.Combine(workspace.Directory, "src"));
        File.WriteAllText(Path.Combine(workspace.Directory, "Zeta.slnx"), "<Solution />");
        File.WriteAllText(Path.Combine(workspace.Directory, "Alpha.sln"), "");
        File.WriteAllText(Path.Combine(workspace.Directory, "src", "Nested.slnx"), "<Solution />");

        var result = await RunInit(workspace.Directory, "\n\n2\n\n");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("1. Alpha.sln", result.Output);
        Assert.Contains("2. Zeta.slnx", result.Output);
        Assert.Contains("3. src/Nested.slnx", result.Output);
        Assert.Contains("solution-path = \"Zeta.slnx\"", File.ReadAllText(Path.Combine(workspace.Directory, "dotnetdo.toml")));
    }

    [Fact]
    public async Task Declines_nested_workspace_by_default_and_reports_ancestor()
    {
        using var workspace = Workspace.Create();
        File.WriteAllText(Path.Combine(workspace.Directory, "dotnetdo.toml"), "scripts-path = \"scripts\"");
        var child = Path.Combine(workspace.Directory, "child");
        Directory.CreateDirectory(child);

        var result = await RunInit(child, "\n");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Initialization cancelled.", result.Error);
        Assert.Contains("The current directory is inside an existing DotNetDo workspace.", result.Output);
        Assert.Contains($"Existing workspace root: {workspace.Directory}", result.Output);
        Assert.Contains($"Create a nested DotNetDo workspace in '{child}'? [y/N]:", result.Output);
        Assert.False(File.Exists(Path.Combine(child, "dotnetdo.toml")));
    }

    [Fact]
    public async Task Creates_nested_workspace_when_confirmed()
    {
        using var workspace = Workspace.Create();
        File.WriteAllText(Path.Combine(workspace.Directory, "dotnetdo.toml"), "scripts-path = \"scripts\"");
        var child = Path.Combine(workspace.Directory, "child");
        Directory.CreateDirectory(child);

        var result = await RunInit(child, "y\n\n\n");

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(child, "dotnetdo.toml")));
        Assert.True(File.Exists(Path.Combine(child, "scripts", "build.cs")));
    }

    [Fact]
    public async Task Existing_initial_script_is_reused_when_creating_configuration()
    {
        using var workspace = Workspace.Create();
        Directory.CreateDirectory(Path.Combine(workspace.Directory, "scripts"));
        File.WriteAllText(Path.Combine(workspace.Directory, "scripts", "build.cs"), "existing");

        var result = await RunInit(workspace.Directory, "\n\n");

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(workspace.Directory, "dotnetdo.toml")));
        Assert.Equal("existing", File.ReadAllText(Path.Combine(workspace.Directory, "scripts", "build.cs")));
    }

    [Fact]
    public async Task Existing_configuration_repairs_launchers_and_syncs_slnx_folder()
    {
        using var workspace = Workspace.Create();
        Directory.CreateDirectory(Path.Combine(workspace.Directory, "scripts", "helpers"));
        File.WriteAllText(Path.Combine(workspace.Directory, "scripts", "build.cs"), "");
        File.WriteAllText(Path.Combine(workspace.Directory, "scripts", "helpers", "shared.cs"), "");
        File.WriteAllText(
            Path.Combine(workspace.Directory, "Product.slnx"),
            """
            <Solution>
              <Folder Name="/Tasks/">
                <File Path="README.md" />
                <File Path="old.cs" />
              </Folder>
            </Solution>
            """);
        const string configuration = "scripts-path = \"scripts\"\nsolution-path = \"Product.slnx\"\nsolution-folder = \"Tasks\"\n";
        File.WriteAllText(Path.Combine(workspace.Directory, "dotnetdo.toml"), configuration);

        var result = await RunInit(workspace.Directory, "\n\n");

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(workspace.Directory, "do")));
        Assert.True(File.Exists(Path.Combine(workspace.Directory, "do.cmd")));
        Assert.Equal(configuration, File.ReadAllText(Path.Combine(workspace.Directory, "dotnetdo.toml")));
        var solution = File.ReadAllText(Path.Combine(workspace.Directory, "Product.slnx"));
        Assert.Contains("scripts/build.cs", solution);
        Assert.Contains("scripts/helpers/shared.cs", solution);
        Assert.DoesNotContain("**", solution);
        Assert.Contains("README.md", solution);
        Assert.DoesNotContain("old.cs", solution);
    }

    [Fact]
    public async Task Existing_configuration_syncs_recursive_sln_files()
    {
        using var workspace = Workspace.Create();
        Directory.CreateDirectory(Path.Combine(workspace.Directory, "scripts", "helpers"));
        File.WriteAllText(Path.Combine(workspace.Directory, "scripts", "build.cs"), "");
        File.WriteAllText(Path.Combine(workspace.Directory, "scripts", "helpers", "shared.cs"), "");
        File.WriteAllText(Path.Combine(workspace.Directory, "Product.sln"), SlnWithTasks);
        File.WriteAllText(
            Path.Combine(workspace.Directory, "dotnetdo.toml"),
            "scripts-path = \"scripts\"\nsolution-path = \"Product.sln\"\nsolution-folder = \"Tasks\"\n");

        var result = await RunInit(workspace.Directory, "\n\n");

        Assert.Equal(0, result.ExitCode);
        var solution = File.ReadAllText(Path.Combine(workspace.Directory, "Product.sln"));
        Assert.Contains("scripts\\build.cs", solution);
        Assert.Contains("scripts\\helpers\\shared.cs", solution);
        Assert.Contains("README.md", solution);
        Assert.DoesNotContain("old.cs", solution);
    }

    [Fact]
    public async Task Existing_configuration_discovers_solution_and_records_default_folder()
    {
        using var workspace = Workspace.Create();
        File.WriteAllText(Path.Combine(workspace.Directory, "Product.slnx"), "<Solution />");
        File.WriteAllText(Path.Combine(workspace.Directory, "dotnetdo.toml"), "scripts-path = \"automation\"");

        var result = await RunInit(workspace.Directory, "\n\n");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            "scripts-path = \"automation\"\nsolution-path = \"Product.slnx\"\nsolution-folder = \"scripts\"\n",
            File.ReadAllText(Path.Combine(workspace.Directory, "dotnetdo.toml")).ReplaceLineEndings("\n"));
        Assert.Contains("automation/build.cs", File.ReadAllText(Path.Combine(workspace.Directory, "Product.slnx")));
        Assert.True(File.Exists(Path.Combine(workspace.Directory, "automation", "build.cs")));
    }

    [Fact]
    public async Task Existing_configuration_can_decline_solution_changes()
    {
        using var workspace = Workspace.Create();
        const string solution = "<Solution />";
        File.WriteAllText(Path.Combine(workspace.Directory, "Product.slnx"), solution);
        File.WriteAllText(
            Path.Combine(workspace.Directory, "dotnetdo.toml"),
            "scripts-path = \"scripts\"\nsolution-path = \"Product.slnx\"\n");

        var result = await RunInit(workspace.Directory, "\nn\n");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(solution, File.ReadAllText(Path.Combine(workspace.Directory, "Product.slnx")));
        Assert.DoesNotContain("solution-folder", File.ReadAllText(Path.Combine(workspace.Directory, "dotnetdo.toml")));
        Assert.True(File.Exists(Path.Combine(workspace.Directory, "do")));
        Assert.True(File.Exists(Path.Combine(workspace.Directory, "do.cmd")));
        Assert.True(File.Exists(Path.Combine(workspace.Directory, "scripts", "build.cs")));
    }

    [Theory]
    [InlineData("do")]
    [InlineData("do.cmd")]
    public async Task Existing_launcher_is_preserved_while_initialization_continues(string launcher)
    {
        using var workspace = Workspace.Create();
        File.WriteAllText(Path.Combine(workspace.Directory, launcher), "existing");

        var result = await RunInit(workspace.Directory, "\n\n");

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(Path.Combine(workspace.Directory, "dotnetdo.toml")));
        Assert.True(File.Exists(Path.Combine(workspace.Directory, "scripts", "build.cs")));
        Assert.Equal("existing", File.ReadAllText(Path.Combine(workspace.Directory, launcher)));
    }

    static async Task<Result> RunInit(string directory, string input)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = directory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "DotNetDo.dll"));
        startInfo.ArgumentList.Add(":init");

        using var process = Process.Start(startInfo)!;
        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        return new(process.ExitCode, output, error);
    }

    sealed record Result(int ExitCode, string Output, string Error);

    const string SlnWithTasks = """
        Microsoft Visual Studio Solution File, Format Version 12.00
        # Visual Studio Version 17
        VisualStudioVersion = 17.0.31903.59
        MinimumVisualStudioVersion = 10.0.40219.1
        Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "Tasks", "Tasks", "{71D8C17B-F42F-44DD-B370-96C31C70D64F}"
            ProjectSection(SolutionItems) = preProject
                README.md = README.md
                old.cs = old.cs
            EndProjectSection
        EndProject
        Global
        EndGlobal
        """;

    sealed class Workspace : IDisposable
    {
        Workspace(string directory) => Directory = directory;

        public string Directory { get; }

        public static Workspace Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"dotnetdo-init-{Guid.NewGuid():N}");
            System.IO.Directory.CreateDirectory(directory);
            return new(directory);
        }

        public void Dispose() => System.IO.Directory.Delete(Directory, recursive: true);
    }
}
