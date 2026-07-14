using System.Diagnostics;
using Xunit;

namespace DotNetDo.Tests;

public sealed class InitCommandTests
{
    [Fact]
    public async Task Initializes_workspace_with_defaults()
    {
        using var workspace = Workspace.Create();

        var result = await RunInit(workspace.Directory, "\n\n");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("scripts-path = \"scripts\"\n", File.ReadAllText(Path.Combine(workspace.Directory, "dotnetdo.toml")).ReplaceLineEndings("\n"));
        Assert.Contains("Hello from build", File.ReadAllText(Path.Combine(workspace.Directory, "scripts", "build.cs")));
        Assert.Contains("Created scripts path: scripts", result.Output);
        Assert.Contains("Run with: do build", result.Output);
    }

    [Fact]
    public async Task Records_the_only_solution_recursively()
    {
        using var workspace = Workspace.Create();
        Directory.CreateDirectory(Path.Combine(workspace.Directory, "src"));
        File.WriteAllText(Path.Combine(workspace.Directory, "src", "Product.slnx"), "<Solution />");

        var result = await RunInit(workspace.Directory, "\n\n");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(
            "scripts-path = \"scripts\"\nsolution-path = \"src/Product.slnx\"\n",
            File.ReadAllText(Path.Combine(workspace.Directory, "dotnetdo.toml")).ReplaceLineEndings("\n"));
        Assert.Contains("Selected solution: src/Product.slnx", result.Output);
    }

    [Fact]
    public async Task Requires_selection_from_solutions_ordered_by_depth_then_name()
    {
        using var workspace = Workspace.Create();
        Directory.CreateDirectory(Path.Combine(workspace.Directory, "src"));
        File.WriteAllText(Path.Combine(workspace.Directory, "Zeta.slnx"), "<Solution />");
        File.WriteAllText(Path.Combine(workspace.Directory, "Alpha.sln"), "");
        File.WriteAllText(Path.Combine(workspace.Directory, "src", "Nested.slnx"), "<Solution />");

        var result = await RunInit(workspace.Directory, "\n\n2\n");

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

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(Path.Combine(workspace.Directory, "dotnetdo.toml"), result.Output);
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
    public async Task Existing_initial_script_fails_without_configuration()
    {
        using var workspace = Workspace.Create();
        Directory.CreateDirectory(Path.Combine(workspace.Directory, "scripts"));
        File.WriteAllText(Path.Combine(workspace.Directory, "scripts", "build.cs"), "existing");

        var result = await RunInit(workspace.Directory, "\n\n");

        Assert.Equal(1, result.ExitCode);
        Assert.False(File.Exists(Path.Combine(workspace.Directory, "dotnetdo.toml")));
        Assert.Equal("existing", File.ReadAllText(Path.Combine(workspace.Directory, "scripts", "build.cs")));
    }

    [Fact]
    public async Task Existing_configuration_fails_without_prompting()
    {
        using var workspace = Workspace.Create();
        File.WriteAllText(Path.Combine(workspace.Directory, "dotnetdo.toml"), "existing");

        var result = await RunInit(workspace.Directory, "");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("already exists", result.Error);
        Assert.Equal("existing", File.ReadAllText(Path.Combine(workspace.Directory, "dotnetdo.toml")));
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
        startInfo.ArgumentList.Add(typeof(Do).Assembly.Location);
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
