using System.IO.Compression;
using Xunit;

namespace DotNetDo.Tests;

public sealed class PathArchiveTests
{
    [Fact]
    public void Zips_a_file_as_one_basename_entry()
    {
        using var workspace = Workspace.Create();
        var source = workspace.Path / "source.txt";
        var destination = workspace.Path / "artifact.data";
        source.WriteText("content");

        var result = source.ZipTo(destination);

        Assert.Equal(destination, result);
        using var archive = ZipFile.OpenRead(destination);
        var entry = Assert.Single(archive.Entries);
        Assert.Equal("source.txt", entry.FullName);
        using var reader = new StreamReader(entry.Open());
        Assert.Equal("content", reader.ReadToEnd());
    }

    [Fact]
    public void Zips_directory_contents_and_empty_directories_without_the_source_name()
    {
        using var workspace = Workspace.Create();
        var source = workspace.Path / "publish";
        (source / "nested").EnsureDirectoryExists();
        (source / "empty").EnsureDirectoryExists();
        (source / "nested/app.dll").WriteText("binary");
        var destination = workspace.Path / "app.zip";

        source.ZipTo(destination, new() { CompressionLevel = CompressionLevel.NoCompression });

        using var archive = ZipFile.OpenRead(destination);
        Assert.Contains(archive.Entries, entry => entry.FullName == "nested/app.dll");
        Assert.Contains(archive.Entries, entry => entry.FullName == "empty/");
        Assert.DoesNotContain(archive.Entries, entry => entry.FullName.StartsWith("publish/", StringComparison.Ordinal));
    }

    [Fact]
    public void Zip_overwrite_preserves_the_old_archive_until_creation_succeeds()
    {
        using var workspace = Workspace.Create();
        var source = workspace.Path / "source";
        source.EnsureDirectoryExists();
        var locked = source / "locked.txt";
        locked.WriteText("content");
        var destination = workspace.Path / "artifact.zip";
        destination.WriteText("original");
        using var lockStream = File.Open(locked, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        Assert.ThrowsAny<IOException>(() => source.ZipTo(destination, new() { Overwrite = true }));

        Assert.Equal("original", destination.ReadText());
        Assert.Equal(["artifact.zip", "source"], Directory.EnumerateFileSystemEntries(workspace.Path)
            .Select(Path.GetFileName)
            .Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Zip_requires_explicit_overwrite_and_an_existing_parent()
    {
        using var workspace = Workspace.Create();
        var source = workspace.Path / "source.txt";
        source.WriteText("new");
        var destination = workspace.Path / "artifact.zip";
        destination.WriteText("old");

        Assert.Throws<IOException>(() => source.ZipTo(destination));
        Assert.Equal("old", destination.ReadText());
        Assert.Throws<DirectoryNotFoundException>(() => source.ZipTo(workspace.Path / "missing/artifact.zip"));

        Assert.Equal(destination, source.ZipTo(destination, new() { Overwrite = true }));
        using var archive = ZipFile.OpenRead(destination);
        Assert.Equal("source.txt", Assert.Single(archive.Entries).FullName);
    }

    [Fact]
    public void Zip_rejects_roots_same_paths_and_destinations_inside_source_directories()
    {
        using var workspace = Workspace.Create();
        var sourceFile = workspace.Path / "source.txt";
        sourceFile.WriteText("content");
        var sourceDirectory = workspace.Path / "source";
        sourceDirectory.EnsureDirectoryExists();
        var root = AbsolutePath.Parse(Path.GetPathRoot(Path.GetTempPath())!);

        Assert.Throws<InvalidOperationException>(() => root.ZipTo(workspace.Path / "root.zip"));
        Assert.Throws<InvalidOperationException>(() => sourceFile.ZipTo(sourceFile, new() { Overwrite = true }));
        Assert.Throws<InvalidOperationException>(() => sourceDirectory.ZipTo(sourceDirectory / "artifact.zip"));
    }

    [Fact]
    public void Unzip_returns_and_creates_the_destination_directory()
    {
        using var workspace = Workspace.Create();
        var archive = CreateArchive(workspace.Path / "artifact.zip",
            ("nested/file.txt", "content"),
            ("empty/", null));
        var destination = workspace.Path / "output";

        var result = archive.UnzipTo(destination);

        Assert.Equal(destination, result);
        Assert.Equal("content", (destination / "nested/file.txt").ReadText());
        Assert.True((destination / "empty").IsExistingDirectory);
    }

    [Fact]
    public void Unzip_without_overwrite_merges_existing_directories()
    {
        using var workspace = Workspace.Create();
        var archive = CreateArchive(workspace.Path / "artifact.zip",
            ("bin/app.dll", "new"),
            ("readme.txt", "new"));
        var destination = workspace.Path / "output";
        (destination / "bin").EnsureDirectoryExists();
        (destination / "unrelated").EnsureDirectoryExists();

        archive.UnzipTo(destination);

        Assert.Equal("new", (destination / "bin/app.dll").ReadText());
        Assert.Equal("new", (destination / "readme.txt").ReadText());
        Assert.True((destination / "unrelated").IsExistingDirectory);
    }

    [Fact]
    public void Unzip_without_overwrite_keeps_unrelated_top_level_entries()
    {
        using var workspace = Workspace.Create();
        var archive = CreateArchive(workspace.Path / "artifact.zip", ("new/file.txt", "new"));
        var destination = workspace.Path / "output";
        (destination / "old").EnsureDirectoryExists();
        (destination / "old/file.txt").WriteText("old");

        archive.UnzipTo(destination);

        Assert.Equal("old", (destination / "old/file.txt").ReadText());
        Assert.Equal("new", (destination / "new/file.txt").ReadText());
    }

    [Fact]
    public void Unzip_without_overwrite_does_not_replace_existing_files()
    {
        using var workspace = Workspace.Create();
        var archive = CreateArchive(workspace.Path / "artifact.zip", ("file.txt", "new"));
        var destination = workspace.Path / "output";
        destination.EnsureDirectoryExists();
        (destination / "file.txt").WriteText("old");

        Assert.Throws<IOException>(() => archive.UnzipTo(destination));

        Assert.Equal("old", (destination / "file.txt").ReadText());
    }

    [Fact]
    public void Unzip_overwrite_merges_directories_and_replaces_files()
    {
        using var workspace = Workspace.Create();
        var archive = CreateArchive(workspace.Path / "artifact.zip",
            ("bin/app.dll", "new"),
            ("new.txt", "new"));
        var destination = workspace.Path / "output";
        (destination / "bin").EnsureDirectoryExists();
        (destination / "bin/app.dll").WriteText("old");
        (destination / "bin/keep.dll").WriteText("keep");

        archive.UnzipTo(destination, new() { Overwrite = true });

        Assert.Equal("new", (destination / "bin/app.dll").ReadText());
        Assert.Equal("keep", (destination / "bin/keep.dll").ReadText());
        Assert.Equal("new", (destination / "new.txt").ReadText());
    }

    [Fact]
    public void Unzip_rejects_escaping_targets()
    {
        using var workspace = Workspace.Create();
        var escaping = CreateArchive(workspace.Path / "escaping.zip",
            ("safe.txt", "safe"),
            ("../escape.txt", "escape"));
        var escapingDestination = workspace.Path / "escaping-output";

        Assert.Throws<IOException>(() => escaping.UnzipTo(escapingDestination, new() { Overwrite = true }));

        Assert.False((workspace.Path / "escape.txt").Exists);
    }

    [Fact]
    public void Unzip_requires_an_existing_destination_parent()
    {
        using var workspace = Workspace.Create();
        var archive = CreateArchive(workspace.Path / "artifact.zip", ("file.txt", "content"));

        Assert.Throws<DirectoryNotFoundException>(() => archive.UnzipTo(workspace.Path / "missing/output"));
        Assert.False((workspace.Path / "missing").Exists);
    }

    static AbsolutePath CreateArchive(AbsolutePath path, params (string Name, string? Contents)[] entries)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, contents) in entries)
        {
            var entry = archive.CreateEntry(name);
            if (contents is not null)
            {
                using var writer = new StreamWriter(entry.Open());
                writer.Write(contents);
            }
        }

        return path;
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
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dotnetdo-archives-{Guid.NewGuid():N}");
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
