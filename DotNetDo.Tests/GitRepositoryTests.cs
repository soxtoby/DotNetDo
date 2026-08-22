using LibGit2Sharp;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace DotNetDo.Tests;

[Collection("Global logger")]
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

    [Fact]
    public async Task VerifyUnchangedAllowsExistingChanges()
    {
        Commit("first");
        File.WriteAllText(Path.Combine(_directory, "tracked.txt"), "existing staged change");
        Commands.Stage(_repository, "tracked.txt");
        File.WriteAllText(Path.Combine(_directory, "tracked.txt"), "existing unstaged change");
        File.WriteAllText(Path.Combine(_directory, "untracked.txt"), "existing untracked change");
        using var git = new GitRepository(AbsolutePath.Parse(_directory));

        await git.VerifyUnchanged(() => { });
        await git.VerifyUnchanged(() => Task.CompletedTask);
    }

    [Fact]
    public async Task VerifyUnchangedDetectsFurtherChangesToDirtyFilesWithoutRestoringThem()
    {
        Commit("first");
        var tracked = Path.Combine(_directory, "tracked.txt");
        var untracked = Path.Combine(_directory, "untracked.txt");
        File.WriteAllText(tracked, "before tracked");
        File.WriteAllText(untracked, "before untracked");
        using var git = new GitRepository(AbsolutePath.Parse(_directory));

        var exception = await Assert.ThrowsAsync<RepositoryChangedException>(() => git.VerifyUnchanged(() =>
            {
                File.WriteAllText(tracked, "after tracked");
                File.WriteAllText(untracked, "after untracked");
            }));

        Assert.Equal(
            [RelativePath.Parse("tracked.txt"), RelativePath.Parse("untracked.txt")],
            exception.ChangedPaths);
        Assert.Contains("tracked.txt", exception.Message);
        Assert.Contains("untracked.txt", exception.Message);
        Assert.Equal("after tracked", File.ReadAllText(tracked));
        Assert.Equal("after untracked", File.ReadAllText(untracked));
    }

    [Fact]
    public async Task VerifyUnchangedDetectsIndexOnlyChanges()
    {
        Commit("first");
        File.WriteAllText(Path.Combine(_directory, "tracked.txt"), "dirty");
        using var git = new GitRepository(AbsolutePath.Parse(_directory));

        var exception = await Assert.ThrowsAsync<RepositoryChangedException>(() => git.VerifyUnchanged(() =>
            Commands.Stage(_repository, "tracked.txt")));

        Assert.Equal([RelativePath.Parse("tracked.txt")], exception.ChangedPaths);
    }

    [Fact]
    public async Task VerifyUnchangedDetectsAddedDeletedAndStagedChanges()
    {
        Commit("first");
        var tracked = Path.Combine(_directory, "tracked.txt");
        File.WriteAllText(tracked, "staged before");
        Commands.Stage(_repository, "tracked.txt");
        using var git = new GitRepository(AbsolutePath.Parse(_directory));

        var exception = await Assert.ThrowsAsync<RepositoryChangedException>(() => git.VerifyUnchanged(() =>
            {
                File.WriteAllText(tracked, "staged after");
                Commands.Stage(_repository, "tracked.txt");
                File.WriteAllText(Path.Combine(_directory, "added.txt"), "added");
                File.Delete(Path.Combine(_directory, "tracked.txt"));
            }));

        Assert.Equal(
            [RelativePath.Parse("added.txt"), RelativePath.Parse("tracked.txt")],
            exception.ChangedPaths);
    }

    [Fact]
    public async Task VerifyUnchangedDetectsFurtherChangesToDirtyBinaryFiles()
    {
        File.WriteAllBytes(Path.Combine(_directory, "binary.dat"), [0, 1, 2]);
        Commands.Stage(_repository, "binary.dat");
        _repository.Commit("binary", _signature, _signature);
        File.WriteAllBytes(Path.Combine(_directory, "binary.dat"), [0, 3, 4]);
        using var git = new GitRepository(AbsolutePath.Parse(_directory));

        var exception = await Assert.ThrowsAsync<RepositoryChangedException>(() => git.VerifyUnchanged(() =>
            File.WriteAllBytes(Path.Combine(_directory, "binary.dat"), [0, 5, 6])));

        Assert.Equal([RelativePath.Parse("binary.dat")], exception.ChangedPaths);
    }

    [Fact]
    public async Task VerifyUnchangedAllowsTemporaryChangesRestoredToTheBaseline()
    {
        Commit("first");
        var path = Path.Combine(_directory, "tracked.txt");
        File.WriteAllText(path, "baseline");
        using var git = new GitRepository(AbsolutePath.Parse(_directory));

        await git.VerifyUnchanged(() =>
            {
                File.WriteAllText(path, "temporary");
                File.WriteAllText(path, "baseline");
            });
    }

    [Fact]
    public async Task VerifyUnchangedUsesGitTextNormalization()
    {
        File.WriteAllText(Path.Combine(_directory, ".gitattributes"), "*.txt text eol=lf\n");
        File.WriteAllText(Path.Combine(_directory, "tracked.txt"), "committed\n");
        Commands.Stage(_repository, [".gitattributes", "tracked.txt"]);
        _repository.Commit("first", _signature, _signature);
        var path = Path.Combine(_directory, "tracked.txt");
        File.WriteAllText(path, "dirty\n");
        using var git = new GitRepository(AbsolutePath.Parse(_directory));

        await git.VerifyUnchanged(() => File.WriteAllText(path, "dirty\r\n"));
    }

    [Fact]
    public async Task VerifyUnchangedPropagatesOperationFailure()
    {
        Commit("first");
        using var git = new GitRepository(AbsolutePath.Parse(_directory));
        var expected = new TestException();

        var actual = await Assert.ThrowsAsync<TestException>(() => git.VerifyUnchanged((Action)(() =>
            {
                File.WriteAllText(Path.Combine(_directory, "new.txt"), "new");
                throw expected;
            })));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task VerifyUnchangedIgnoresIgnoredFiles()
    {
        File.WriteAllText(Path.Combine(_directory, ".gitignore"), "ignored.txt\n");
        Commands.Stage(_repository, ".gitignore");
        _repository.Commit("ignore file", _signature, _signature);
        using var git = new GitRepository(AbsolutePath.Parse(_directory));

        await git.VerifyUnchanged(() => File.WriteAllText(Path.Combine(_directory, "ignored.txt"), "ignored"));
    }

    [Fact]
    public async Task VerifyUnchangedIgnoresIndexImplementationFlags()
    {
        Commit("first");
        using var git = new GitRepository(AbsolutePath.Parse(_directory));

        await git.VerifyUnchanged(async () =>
            await git.Exec("update-index --assume-unchanged tracked.txt"));
    }

    [Fact]
    public async Task VerifyUnchangedIgnoresRefChangesWhenTreesMatch()
    {
        Commit("first");
        File.WriteAllText(Path.Combine(_directory, "tracked.txt"), "committed during operation");
        Commands.Stage(_repository, "tracked.txt");
        using var git = new GitRepository(AbsolutePath.Parse(_directory));

        await git.VerifyUnchanged(() => _repository.Commit("second", _signature, _signature));
    }

    [Fact]
    public async Task VerifyUnchangedLogsShortRedactedSnippets()
    {
        File.WriteAllText(Path.Combine(_directory, "tracked.txt"), string.Join('\n', Enumerable.Range(1, 20).Select(i => $"before {i}")));
        Commands.Stage(_repository, "tracked.txt");
        _repository.Commit("first", _signature, _signature);
        using var git = new GitRepository(AbsolutePath.Parse(_directory));
        var previous = Log.Logger;
        var sink = new CapturingSink();
        var logger = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .CreateRedactingLogger();
        var secret = $"secret-{Guid.NewGuid():N}";
        _ = new Secret(secret);

        try
        {
            Log.Logger = logger;

            await Assert.ThrowsAsync<RepositoryChangedException>(() => git.VerifyUnchanged(() =>
                File.WriteAllText(
                    Path.Combine(_directory, "tracked.txt"),
                    string.Join('\n', Enumerable.Range(1, 20).Select(i => i == 10 ? secret : $"before {i}")))));

            var message = Assert.Single(sink.Events, logEvent => logEvent.Level == LogEventLevel.Error).RenderMessage();
            Assert.Contains("tracked.txt", message);
            Assert.Contains("***", message);
            Assert.DoesNotContain(secret, message);
            Assert.InRange(message.SplitLines().Count(line => line.StartsWith(" ") || line.StartsWith("+") || line.StartsWith("-")), 1, 12);
        }
        finally
        {
            Log.Logger = previous;
            (logger as IDisposable)?.Dispose();
        }
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

    sealed class CapturingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    sealed class TestException : Exception;
}
