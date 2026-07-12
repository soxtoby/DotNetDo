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
        Do.WorkingDirectory = child;

        Assert.Equal(child, Do.RootDirectory);

        File.WriteAllText(outer / "dotnetdo.toml", "invalid TOML still marks the root");
        File.WriteAllText(inner / "dotnetdo.toml", "");

        Assert.Equal(inner, Do.RootDirectory);

        File.Delete(inner / "dotnetdo.toml");
        Do.WorkingDirectory = outer;
        Assert.Equal(inner, Do.RootDirectory);
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
