using Xunit;
using System.Text;
using System.Text.Json;

namespace DotNetDo.Tests;

public sealed class PathFileSystemTests
{
    [Fact]
    public void Classifies_and_ensures_directories()
    {
        using var workspace = Workspace.Create();
        var directory = workspace.Path / "a/b";
        var file = workspace.Path / "file.txt";

        Assert.False(directory.Exists);
        Assert.Same(directory, directory.EnsureDirectoryExists());
        File.WriteAllText(file, "content");

        Assert.True(directory.Exists);
        Assert.True(directory.IsExistingDirectory);
        Assert.False(directory.IsExistingFile);
        Assert.True(file.Exists);
        Assert.True(file.IsExistingFile);
        Assert.False(file.IsExistingDirectory);
        Assert.Throws<IOException>(file.EnsureDirectoryExists);
    }

    [Fact]
    public void Gets_relative_path_to_another_absolute_path()
    {
        using var workspace = Workspace.Create();

        Assert.Equal(RelativePath.Parse("a/b.txt"), workspace.Path.RelativePathTo(workspace.Path / "a/b.txt"));
        Assert.Equal(RelativePath.Parse("../c.txt"), (workspace.Path / "a/b.txt").RelativePathTo(workspace.Path / "a/c.txt"));
    }

    [Fact]
    public void Containment_requires_the_same_root()
    {
        Assert.True(AbsolutePath.Parse("C:/a/b").IsWithin(AbsolutePath.Parse("C:/a")));
        Assert.False(AbsolutePath.Parse("C:/a/b").IsWithin(AbsolutePath.Parse("D:/a")));
    }

    [Fact]
    public void Globs_files_and_directories_with_ordered_patterns()
    {
        using var workspace = Workspace.Create();
        (workspace.Path / "src/a").EnsureDirectoryExists();
        (workspace.Path / "src/obj").EnsureDirectoryExists();
        File.WriteAllText(workspace.Path / "src/a/one.cs", "");
        File.WriteAllText(workspace.Path / "src/a/two.txt", "");
        File.WriteAllText(workspace.Path / "src/obj/generated.cs", "");

        var files = workspace.Path.GlobFiles(["src/**/*.cs", "!**/obj/**", "src/obj/generated.cs"]);
        var directories = workspace.Path.GlobDirectories(["src/**"]);

        Assert.Equal([workspace.Path / "src/a/one.cs", workspace.Path / "src/obj/generated.cs"], files);
        Assert.Equal([workspace.Path / "src/a", workspace.Path / "src/obj"], directories);
    }

    [Fact]
    public void Copies_moves_and_deletes_files()
    {
        using var workspace = Workspace.Create();
        var source = workspace.Path / "source.txt";
        var container = workspace.Path / "copies";
        File.WriteAllText(source, "one");

        var copied = source.CopyInto(container, new() { CreateDirectories = true });
        Assert.Equal(container / "source.txt", copied);
        Assert.Equal("one", File.ReadAllText(copied));

        File.WriteAllText(source, "two");
        source.CopyTo(copied, new() { Overwrite = true });
        Assert.Equal("two", File.ReadAllText(copied));

        var moved = copied.MoveTo(workspace.Path / "moved.txt");
        Assert.False(copied.Exists);
        Assert.True(moved.Exists);

        moved.Delete();
        moved.Delete();
        Assert.False(moved.Exists);
    }

    [Fact]
    public void Copies_and_merges_directory_trees()
    {
        using var workspace = Workspace.Create();
        var source = workspace.Path / "source";
        var destination = workspace.Path / "destination";
        (source / "nested").EnsureDirectoryExists();
        destination.EnsureDirectoryExists();
        File.WriteAllText(source / "nested/new.txt", "new");
        File.WriteAllText(destination / "old.txt", "old");

        source.CopyTo(destination, new() { Overwrite = true });

        Assert.Equal("old", File.ReadAllText(destination / "old.txt"));
        Assert.Equal("new", File.ReadAllText(destination / "nested/new.txt"));
        destination.Delete();
        Assert.False(destination.Exists);
    }

    [Fact]
    public void Directory_copy_requires_destination_directories_unless_creation_is_enabled()
    {
        using var workspace = Workspace.Create();
        var source = workspace.Path / "source";
        source.EnsureDirectoryExists();

        Assert.Throws<DirectoryNotFoundException>(() => source.CopyTo(workspace.Path / "missing/destination"));
        Assert.Throws<DirectoryNotFoundException>(() => source.CopyInto(workspace.Path / "container"));
        Assert.False((workspace.Path / "missing").Exists);
        Assert.False((workspace.Path / "container").Exists);
    }

    [Fact]
    public void Copies_directory_beneath_itself_without_recursing_forever()
    {
        using var workspace = Workspace.Create();
        var source = workspace.Path / "source";
        source.EnsureDirectoryExists();

        File.WriteAllText(source / "file.txt", "content");

        source.CopyTo(source / "child", new() { CreateDirectories = true });

        Assert.Equal("content", File.ReadAllText(source / "child/file.txt"));
        Assert.False((source / "child/child").Exists);
    }

    [Fact]
    public void Reads_and_writes_text_and_lines()
    {
        using var workspace = Workspace.Create();
        var text = workspace.Path / "text.txt";
        var lines = workspace.Path / "lines.txt";

        text.WriteText("héllo", Encoding.Unicode);
        lines.WriteLines(["one", "two"]);

        Assert.Equal("héllo", text.ReadText(Encoding.Unicode));
        Assert.Equal(["one", "two"], lines.ReadLines());
    }

    [Fact]
    public void Reads_and_writes_structured_values()
    {
        using var workspace = Workspace.Create();
        var value = new ContentModel { Name = "test", Count = 2 };
        var json = workspace.Path / "value.json";
        var toml = workspace.Path / "value.toml";
        var xml = workspace.Path / "value.xml";

        json.WriteJson(value, new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        toml.WriteToml(value);
        xml.WriteXml(value);

        Assert.Equal(value, json.ReadJson<ContentModel>(new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        Assert.Equal(value, toml.ReadToml<ContentModel>());
        Assert.Equal(value, xml.ReadXml<ContentModel>());
    }

    [Fact]
    public void Writes_require_an_existing_parent_directory()
    {
        using var workspace = Workspace.Create();
        var file = workspace.Path / "missing/value.txt";

        Assert.Throws<DirectoryNotFoundException>(() => file.WriteText("value"));
        Assert.False(file.Parent!.Exists);
    }

    public sealed record ContentModel
    {
        public string Name { get; set; } = "";
        public int Count { get; set; }
    }

    sealed class Workspace : IDisposable
    {
        Workspace(string directory)
        {
            Directory = directory;
            Path = AbsolutePath.Parse(directory);
        }

        public string Directory { get; }
        public AbsolutePath Path { get; }

        public static Workspace Create()
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dotnetdo-paths-{Guid.NewGuid():N}");
            System.IO.Directory.CreateDirectory(directory);
            return new(directory);
        }

        public void Dispose()
        {
            if (System.IO.Directory.Exists(Directory))
                System.IO.Directory.Delete(Directory, recursive: true);
        }
    }
}
