using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace DotNetDo.Tests;

/// <summary>
/// Tasks run as file-based apps, which build with Native AOT publishing defaults. DotNetDo.Core.props
/// re-enables reflection-based TOML serialization for them, but consumers may opt back out, so loading
/// configuration must not depend on it. These tests load configuration in a child process with the switch
/// off, because both the test host and the tool itself run with reflection-based serialization enabled.
/// </summary>
public sealed class ScriptConfigurationTests
{
    [Fact]
    public async Task Configuration_loads_without_reflection_based_serialization()
    {
        using var workspace = Workspace.Create();
        await File.WriteAllTextAsync(
            Path.Combine(workspace.Directory, "dotnetdo.toml"),
            """
            scripts-path = "automation"

            [tasks]
            ci = ["build", "pack"]

            [build]
            configuration = "Release"
            """,
            TestContext.Current.CancellationToken);
        Directory.CreateDirectory(Path.Combine(workspace.Directory, "automation"));
        await File.WriteAllTextAsync(Path.Combine(workspace.Directory, "automation", "build.cs"), "// build", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(Path.Combine(workspace.Directory, "automation", "pack.cs"), "// pack", TestContext.Current.CancellationToken);

        var result = await ListTasks(workspace);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal(["build", "ci", "pack"], result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
        Assert.Empty(result.Error);
    }

    [Fact]
    public async Task Invalid_configuration_still_fails_without_reflection_based_serialization()
    {
        using var workspace = Workspace.Create();
        await File.WriteAllTextAsync(
            Path.Combine(workspace.Directory, "dotnetdo.toml"),
            "unknown-setting = \"value\"",
            TestContext.Current.CancellationToken);

        var result = await ListTasks(workspace);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Unknown DotNetDo setting 'unknown-setting'", result.Error);
    }

    /// <summary>Lists the workspace's tasks, which requires loading its configuration.</summary>
    static async Task<Result> ListTasks(Workspace workspace)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workspace.Directory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add("--runtimeconfig");
        startInfo.ArgumentList.Add(workspace.RuntimeConfigFile);
        startInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "DotNetDo.dll"));

        using var process = Process.Start(startInfo)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        return new(process.ExitCode, output, error);
    }

    sealed class Workspace : IDisposable
    {
        Workspace(string directory, string runtimeConfigFile)
        {
            Directory = directory;
            RuntimeConfigFile = runtimeConfigFile;
        }

        public string Directory { get; }

        /// <summary>The tool's own runtime configuration, with reflection-based TOML serialization opted out.</summary>
        public string RuntimeConfigFile { get; }

        public static Workspace Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"dotnetdo-script-{Guid.NewGuid():N}");
            System.IO.Directory.CreateDirectory(directory);
            return new(directory, WriteRuntimeConfig(directory));
        }

        static string WriteRuntimeConfig(string directory)
        {
            var source = Path.Combine(AppContext.BaseDirectory, "DotNetDo.runtimeconfig.json");
            var runtimeConfig = JsonNode.Parse(File.ReadAllText(source))!;
            var configProperties = runtimeConfig["runtimeOptions"]!["configProperties"] ??= new JsonObject();
            configProperties["Tomlyn.TomlSerializer.IsReflectionEnabledByDefault"] = false;

            var target = Path.Combine(directory, "DotNetDo.runtimeconfig.json");
            File.WriteAllText(target, runtimeConfig.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return target;
        }

        public void Dispose() => System.IO.Directory.Delete(Directory, recursive: true);
    }

    sealed record Result(int ExitCode, string Output, string Error);
}
