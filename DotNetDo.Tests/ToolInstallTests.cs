using Xunit;

namespace DotNetDo.Tests;

public sealed class ToolInstallTests
{
    [Fact]
    public void Azure_requirement_maps_canonical_name_to_executable_and_scoop_app()
    {
        var install = Tools.Azure.Install;

        Assert.Equal("azure", install.ToolName);
        Assert.Equal("az", install.ExecutableName);
        Assert.Equal("azure-cli", install.ScoopApp);
    }

    [Fact]
    public void Scoop_app_is_optional()
    {
        var install = new ToolInstall("custom", "custom");

        Assert.Null(install.ScoopApp);
    }

    [Fact]
    public void Tool_availability_finds_commands_on_the_search_path()
    {
        Assert.True(ToolInstall.CommandExists("dotnet"));
        Assert.False(ToolInstall.CommandExists("dotnetdo-missing-tool"));
    }

    [Fact]
    public void Tool_availability_resolves_windows_script_shims_without_extensions()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var directory = Path.Combine(Path.GetTempPath(), $"dotnetdo-locator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var originalPath = Environment.GetEnvironmentVariable("PATH");
        try
        {
            File.WriteAllText(Path.Combine(directory, "fake-shim.cmd"), "@echo fake");
            Environment.SetEnvironmentVariable("PATH", $"{directory}{Path.PathSeparator}{originalPath}");

            Assert.True(ToolInstall.CommandExists("fake-shim"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Exec_runs_windows_batch_shims_found_on_the_search_path()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var directory = Path.Combine(Path.GetTempPath(), $"dotnetdo batch shim {Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var originalPath = Environment.GetEnvironmentVariable("PATH");
        try
        {
            File.WriteAllText(Path.Combine(directory, "fake-shim.cmd"), "@echo %*");
            Environment.SetEnvironmentVariable("PATH", $"{directory}{Path.PathSeparator}{originalPath}");

            var result = await Do.Exec("fake-shim one two", new ExecOptions { Log = (_, _) => { } });

            Assert.Equal("one two", result.ReadText());
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Satisfied_requirement_installs_as_successful_no_op()
    {
        var install = new ToolInstall("dotnet", "dotnet") { ScoopApp = "dotnet-sdk" };

        Assert.True(install.IsAvailable);
        await install;
    }

    [Fact]
    public async Task Missing_requirement_fails_on_unsupported_platforms()
    {
        if (OperatingSystem.IsWindows())
            return;

        var install = new ToolInstall("missing", "dotnetdo-missing-tool") { ScoopApp = "dotnetdo-missing-tool" };

        await Assert.ThrowsAsync<ToolInstallException>(async () => await install);
    }

    [Fact]
    public async Task Missing_requirement_without_scoop_app_fails_clearly()
    {
        var install = new ToolInstall("missing", "dotnetdo-missing-tool");

        var exception = await Assert.ThrowsAsync<ToolInstallException>(async () => await install);
        Assert.Equal("'missing' is unavailable and has no configured installer.", exception.Message);
    }
}
