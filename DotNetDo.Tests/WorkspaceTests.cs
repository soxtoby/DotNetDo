using Xunit;

namespace DotNetDo.Tests;

[Collection("Working directory")]
public sealed class WorkspaceTests
{
    [Fact]
    public void Working_directory_wraps_process_working_directory()
    {
        using var workspace = Workspace.Create();

        Do.WorkingDirectory = workspace.Path;

        Assert.Equal(workspace.Path, Do.WorkingDirectory);
        Assert.Equal((string)workspace.Path, Environment.CurrentDirectory);
    }

    [Fact]
    public void Root_directory_falls_back_until_configured_then_remains_stable()
    {
        using var workspace = Workspace.Create();
        var outer = workspace.Path / "outer";
        var inner = outer / "inner";
        var child = inner / "child";
        child.EnsureDirectoryExists();
        var rootDirectory = new WorkspaceRoot();

        Assert.Equal(child, rootDirectory.Resolve(child));

        File.WriteAllText(outer / "dotnetdo.toml", "invalid TOML still marks the root");
        File.WriteAllText(inner / "dotnetdo.toml", "");

        Assert.Equal(inner, rootDirectory.Resolve(child));

        File.Delete(inner / "dotnetdo.toml");
        Assert.Equal(inner, rootDirectory.Resolve(outer));
    }

    [Fact]
    public void Scripts_path_defaults_to_scripts()
    {
        using var workspace = Workspace.Create();

        Assert.Equal(RelativePath.Parse("scripts"), WorkspaceConfiguration.ReadScriptsPath(workspace.Path));
    }

    [Theory]
    [InlineData("scripts-path = \"build/tasks\"", "build/tasks")]
    [InlineData("scripts-path = \".\"", ".")]
    [InlineData("[parameters]\nconfiguration = \"Release\"", "scripts")]
    public void Scripts_path_reads_configuration(string configuration, string expected)
    {
        using var workspace = Workspace.Create();
        File.WriteAllText(workspace.Path / "dotnetdo.toml", configuration);

        Assert.Equal(RelativePath.Parse(expected), WorkspaceConfiguration.ReadScriptsPath(workspace.Path));
    }

    [Theory]
    [InlineData("script-path = \"scripts\"")]
    [InlineData("scripts-path = \"\"")]
    [InlineData("scripts-path = \"../scripts\"")]
    [InlineData("scripts-path = \"C:\\\\scripts\"")]
    [InlineData("not valid TOML")]
    public void Invalid_scripts_configuration_fails(string configuration)
    {
        using var workspace = Workspace.Create();
        File.WriteAllText(workspace.Path / "dotnetdo.toml", configuration);

        Assert.Throws<DotNetDoConfigurationException>(() => WorkspaceConfiguration.ReadScriptsPath(workspace.Path));
    }

    sealed class Workspace : IDisposable
    {
        readonly AbsolutePath _originalWorkingDirectory;

        Workspace(AbsolutePath path)
        {
            Path = path;
            _originalWorkingDirectory = Do.WorkingDirectory;
        }

        public AbsolutePath Path { get; }

        public static Workspace Create()
        {
            var path = AbsolutePath.Parse(System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dotnetdo-workspace-{Guid.NewGuid():N}"));
            path.EnsureDirectoryExists();
            return new(path);
        }

        public void Dispose()
        {
            Do.WorkingDirectory = _originalWorkingDirectory;
            Path.Delete();
        }
    }
}

[CollectionDefinition("Working directory", DisableParallelization = true)]
public sealed class WorkingDirectoryCollection;
