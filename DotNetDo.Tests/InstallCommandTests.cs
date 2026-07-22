using System.Diagnostics;
using Xunit;

namespace DotNetDo.Tests;

public sealed class InstallCommandTests
{
    [Fact]
    public async Task No_declared_tools_succeeds_trivially()
    {
        using var workspace = Workspace.Create();
        File.WriteAllText(Path.Combine(workspace.Directory, "dotnetdo.toml"), "");

        var result = await RunInstall(workspace.Directory);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("No tool requirements are declared in dotnetdo.toml.", result.Output);
    }

    [Fact]
    public async Task Missing_configuration_succeeds_trivially()
    {
        using var workspace = Workspace.Create();

        var result = await RunInstall(workspace.Directory);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("No tool requirements are declared in dotnetdo.toml.", result.Output);
    }

    [Fact]
    public async Task Unknown_tool_name_fails_as_invalid_configuration()
    {
        using var workspace = Workspace.Create();
        File.WriteAllText(Path.Combine(workspace.Directory, "dotnetdo.toml"), "tools = [\"unknown\"]");

        var result = await RunInstall(workspace.Directory);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Unknown tool 'unknown'", result.Error);
    }

    [Fact]
    public async Task Arguments_fail_with_usage()
    {
        using var workspace = Workspace.Create();

        var result = await RunInstall(workspace.Directory, "azure");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Usage: dotnet do :install", result.Error);
    }

    static async Task<Result> RunInstall(string directory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "DotNetDo.dll"));
        startInfo.ArgumentList.Add(":install");
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)!;
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
            var directory = Path.Combine(Path.GetTempPath(), $"dotnetdo-install-{Guid.NewGuid():N}");
            System.IO.Directory.CreateDirectory(directory);
            return new(directory);
        }

        public void Dispose() => System.IO.Directory.Delete(Directory, recursive: true);
    }
}
