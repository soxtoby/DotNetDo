using LibGit2Sharp;
using Xunit;

namespace DotNetDo.Tests;

public sealed class GitRepositoryTests : IDisposable
{
    readonly string _directory = Path.Combine(Do.WorkingDirectory, ".test-workspaces", $"git-{Guid.NewGuid():N}");
    readonly Repository _repository;
    readonly Signature _signature = new("DotNetDo Tests", "tests@dotnetdo.test", DateTimeOffset.Now);

    public GitRepositoryTests()
    {
        Directory.CreateDirectory(_directory);
        Repository.Init(_directory);
        _repository = new Repository(_directory);
        _repository.Config.Set("user.name", _signature.Name);
        _repository.Config.Set("user.email", _signature.Email);
    }

    [Fact]
    public void DiscoversFromChildAndReadsLiveState()
    {
        var child = Directory.CreateDirectory(Path.Combine(_directory, "src"));
        using var git = new GitRepository(AbsolutePath.Parse(child.FullName));

        Assert.Equal(AbsolutePath.Parse(_directory), git.Root);
        Assert.Throws<InvalidOperationException>(() => git.CurrentCommit);

        File.WriteAllText(Path.Combine(_directory, "new file.txt"), "content");
        var change = Assert.Single(git.Changes);
        Assert.Equal(RelativePath.Parse("new file.txt"), change.Path);
        Assert.True(change.State.HasFlag(FileStatus.NewInWorkdir));
        Assert.True(git.IsDirty);
    }

    [Fact]
    public void IgnoredFilesAreNotDirty()
    {
        File.WriteAllText(Path.Combine(_directory, ".gitignore"), "ignored.txt\n");
        Commands.Stage(_repository, ".gitignore");
        _repository.Commit("ignore file", _signature, _signature);
        File.WriteAllText(Path.Combine(_directory, "ignored.txt"), "content");
        using var git = new GitRepository(AbsolutePath.Parse(_directory));

        Assert.False(git.IsDirty);
    }

    [Fact]
    public void CommitsSinceWalksHeadBackToMergeBase()
    {
        var first = Commit("first");
        _repository.Branches.Add("base", first);
        var second = Commit("second");
        using var git = new GitRepository(AbsolutePath.Parse(_directory));

        var commits = git.CommitsSince("base");

        Assert.Equal([second.Id], commits.Select(commit => commit.Id));
    }

    [Fact]
    public async Task AddAndResetOperateOnWholePaths()
    {
        File.WriteAllText(Path.Combine(_directory, "new file.txt"), "content");
        using var git = new GitRepository(AbsolutePath.Parse(_directory));

        await (git.Add with { Paths = [RelativePath.Parse("new file.txt")] });
        Assert.True(Assert.Single(git.Changes).State.HasFlag(FileStatus.NewInIndex));

        await (git.Reset with { All = true });
        Assert.True(Assert.Single(git.Changes).State.HasFlag(FileStatus.NewInWorkdir));
    }

    [Fact]
    public async Task CommitAllUsesAuthorOverride()
    {
        Commit("first");
        File.WriteAllText(Path.Combine(_directory, "tracked.txt"), "changed");
        using var git = new GitRepository(AbsolutePath.Parse(_directory));

        await (git.Commit with
            {
                Message = "changed tracked file",
                All = true,
                Author = new GitAuthor("Build Author", "build@author.test")
            });

        Assert.Equal("Build Author", git.CurrentCommit.Author.Name);
        Assert.Equal("build@author.test", git.CurrentCommit.Author.Email);
    }

    [Fact]
    public async Task CreateTagCreatesAnnotatedTagAtTarget()
    {
        var first = Commit("first");
        var second = Commit("second");
        using var git = new GitRepository(AbsolutePath.Parse(_directory));

        await (git.CreateTag with { Name = "v1.0.0", Message = "Version 1.0.0", Target = first });

        var tag = git.Tags["v1.0.0"];
        Assert.NotNull(tag);
        Assert.Equal(first.Id, tag.Target.Peel<Commit>().Id);
        Assert.NotEqual(second.Id, tag.Target.Peel<Commit>().Id);
    }

    [Fact]
    public void CommandsRequireExactlyOnePathMode()
    {
        using var git = new GitRepository(AbsolutePath.Parse(_directory));

        Assert.Throws<InvalidOperationException>(() => git.Add.ToString());
        Assert.Throws<InvalidOperationException>(() => (git.Add with { All = true, Paths = [RelativePath.Parse("file")] }).ToString());
    }

    Commit Commit(string message)
    {
        var path = Path.Combine(_directory, "tracked.txt");
        File.AppendAllText(path, message);
        Commands.Stage(_repository, "tracked.txt");
        return _repository.Commit(message, _signature, _signature);
    }

    public void Dispose()
    {
        _repository.Dispose();
        foreach (var file in Directory.EnumerateFiles(_directory, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(_directory, recursive: true);
    }
}
