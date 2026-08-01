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
    public void Special_folder_properties_wrap_configured_paths()
    {
        (Func<AbsolutePath> Get, Environment.SpecialFolder Folder)[] properties =
        [
            (() => Do.UserProfile, Environment.SpecialFolder.UserProfile),
            (() => Do.Documents, Environment.SpecialFolder.MyDocuments),
            (() => Do.ApplicationData, Environment.SpecialFolder.ApplicationData),
            (() => Do.LocalApplicationData, Environment.SpecialFolder.LocalApplicationData),
            (() => Do.ProgramFiles, Environment.SpecialFolder.ProgramFiles),
            (() => Do.ProgramFilesX86, Environment.SpecialFolder.ProgramFilesX86),
        ];

        foreach (var (get, folder) in properties)
        {
            var configured = Environment.GetFolderPath(folder, Environment.SpecialFolderOption.DoNotVerify);
            if (string.IsNullOrEmpty(configured))
                Assert.Throws<DirectoryNotFoundException>(() => get());
            else
                Assert.Equal(AbsolutePath.Parse(configured), get());
        }
    }

    [Fact]
    public void Empty_special_folder_path_throws_when_used()
    {
        var exception = Assert.Throws<DirectoryNotFoundException>(
            () => SpecialFolders.Parse("", Environment.SpecialFolder.ProgramFilesX86));

        Assert.Equal("ProgramFilesX86 directory is unavailable.", exception.Message);
    }

    [Fact]
    public void Creates_unique_temp_directory_with_prefix()
    {
        var directory = Do.CreateTempDirectory("dotnetdo-directory-");

        try
        {
            Assert.True(directory.IsExistingDirectory);
            Assert.StartsWith("dotnetdo-directory-", directory.Name);
        }
        finally
        {
            Directory.Delete(directory);
        }
    }

    [Fact]
    public void Creates_unique_empty_temp_file_with_prefix()
    {
        var file = Do.CreateTempFile("dotnetdo-file-", ".json");

        try
        {
            Assert.True(file.IsExistingFile);
            Assert.StartsWith("dotnetdo-file-", file.Name);
            Assert.Equal(".json", file.Extension);
            Assert.Equal(0, new FileInfo(file).Length);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Temp_artifact_prefix_must_be_a_file_name()
    {
        Assert.Throws<ArgumentException>(() => Do.CreateTempDirectory("nested/path"));
        Assert.Throws<ArgumentException>(() => Do.CreateTempFile(@"nested\path"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("json")]
    [InlineData(".")]
    [InlineData(".nested/json")]
    public void Temp_file_extension_must_be_dot_prefixed(string extension)
    {
        Assert.Throws<ArgumentException>(() => Do.CreateTempFile(extension: extension));
    }

    [Fact]
    public void Configuration_lookup_finds_the_nearest_marker()
    {
        using var workspace = Workspace.Create();
        var outer = workspace.Path / "outer";
        var inner = outer / "inner";
        var child = inner / "child";
        child.EnsureDirectoryExists();
        Assert.Null(WorkspaceConfiguration.FindClosest(child));

        File.WriteAllText(outer / "dotnetdo.toml", "invalid TOML still marks the root");
        File.WriteAllText(inner / "dotnetdo.toml", "");

        Assert.Equal(inner / "dotnetdo.toml", WorkspaceConfiguration.FindClosest(child));

        File.Delete(inner / "dotnetdo.toml");
        Assert.Equal(outer / "dotnetdo.toml", WorkspaceConfiguration.FindClosest(outer));
    }

    [Fact]
    public void Scripts_path_defaults_to_scripts()
    {
        using var workspace = Workspace.Create();

        Assert.Equal(RelativePath.Parse("scripts"), WorkspaceConfiguration.Load(workspace.Path).ScriptsPath);
    }

    [Theory]
    [InlineData("scripts-path = \"build/tasks\"", "build/tasks")]
    [InlineData("scripts-path = \".\"", ".")]
    [InlineData("[parameters]\nconfiguration = \"Release\"", "scripts")]
    public void Scripts_path_reads_configuration(string configuration, string expected)
    {
        using var workspace = Workspace.Create();
        File.WriteAllText(workspace.Path / "dotnetdo.toml", configuration);

        Assert.Equal(RelativePath.Parse(expected), WorkspaceConfiguration.Load(workspace.Path).ScriptsPath);
    }

    [Fact]
    public void Loads_typed_workspace_configuration()
    {
        using var workspace = Workspace.Create();
        File.WriteAllText(
            workspace.Path / "dotnetdo.toml",
            """
            scripts-path = "automation"
            solution-path = "Product.slnx"
            solution-folder = "Tasks"

            [tasks]
            test = ["build", "test-csharp --no-build"]

            [build]
            configuration = "Release"
            """);

        var configuration = WorkspaceConfiguration.Load(workspace.Path);

        Assert.Equal(RelativePath.Parse("automation"), configuration.ScriptsPath);
        Assert.Equal(RelativePath.Parse("Product.slnx"), configuration.SolutionPath);
        Assert.Equal("Tasks", configuration.SolutionFolder);
        Assert.Equal(new[] { "build", "test-csharp --no-build" }, configuration.MetaTasks["test"]);
    }

    [Fact]
    public void Loads_declared_tool_requirements()
    {
        using var workspace = Workspace.Create();
        File.WriteAllText(workspace.Path / "dotnetdo.toml", "tools = [\"azure\", \"bun\"]");

        Assert.Equal([Tools.Azure.EnsureAvailable, Tools.Bun.EnsureAvailable], WorkspaceConfiguration.Load(workspace.Path).Tools);
    }

    [Fact]
    public void Tool_requirements_default_to_none()
    {
        using var workspace = Workspace.Create();
        File.WriteAllText(workspace.Path / "dotnetdo.toml", "");

        Assert.Empty(WorkspaceConfiguration.Load(workspace.Path).Tools);
    }

    [Theory]
    [InlineData("tools = \"azure\"")]
    [InlineData("tools = [1]")]
    [InlineData("tools = [\"unknown\"]")]
    [InlineData("tools = [\"Azure\"]")]
    [InlineData("tools = [\"azure\", \"azure\"]")]
    public void Invalid_tool_requirements_fail(string configuration)
    {
        using var workspace = Workspace.Create();
        File.WriteAllText(workspace.Path / "dotnetdo.toml", configuration);

        Assert.Throws<DotNetDoConfigurationException>(() => WorkspaceConfiguration.Load(workspace.Path));
    }

    [Theory]
    [InlineData("script-path = \"scripts\"")]
    [InlineData("scripts-path = \"\"")]
    [InlineData("scripts-path = \"../scripts\"")]
    [InlineData("scripts-path = \"C:\\\\scripts\"")]
    [InlineData("not valid TOML")]
    [InlineData("solution-folder = \"\"")]
    [InlineData("solution-folder = \"nested/tasks\"")]
    public void Invalid_scripts_configuration_fails(string configuration)
    {
        using var workspace = Workspace.Create();
        File.WriteAllText(workspace.Path / "dotnetdo.toml", configuration);

        Assert.Throws<DotNetDoConfigurationException>(() => WorkspaceConfiguration.Load(workspace.Path));
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
