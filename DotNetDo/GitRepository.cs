using LibGit2Sharp;

namespace DotNetDo;

public static partial class Do
{
    public static GitRepository GitRepo
    {
        get
        {
            if (field is not null)
                return field;

            var repository = field = new GitRepository(RootDirectory);
            AppDomain.CurrentDomain.ProcessExit += (_, _) => Interlocked.Exchange(ref repository, null).Dispose();
            return repository;
        }
        set => field = value ?? throw new ArgumentNullException(nameof(value));
    }
}

public sealed class GitRepository : IDisposable
{
    public GitRepository(AbsolutePath directory)
    {
        ArgumentNullException.ThrowIfNull(directory);
        
        if (!directory.IsExistingDirectory)
            throw new DirectoryNotFoundException($"Git repository discovery requires an existing directory: '{directory}'.");

        var repositoryPath = Repository.Discover(directory)
            ?? throw new RepositoryNotFoundException($"No Git repository was found in '{directory}' or its ancestors.");
        Repository = new Repository(repositoryPath);

        if (Repository.Info.IsBare || string.IsNullOrWhiteSpace(Repository.Info.WorkingDirectory))
        {
            Repository.Dispose();
            throw new NotSupportedException("Bare Git repositories are not supported.");
        }

        Root = AbsolutePath.Parse(Path.TrimEndingDirectorySeparator(Repository.Info.WorkingDirectory));
        Add = new(this);
        Reset = new(this);
        Commit = new(this);
        Push = new(this);
        CreateTag = new(this);
        PushTag = new(this);
    }

    public AbsolutePath Root { get; }
    public string? CurrentBranch => Repository.Info.IsHeadDetached ? null : Repository.Head.FriendlyName;
    public Commit CurrentCommit => Repository.Head.Tip
        ?? throw new InvalidOperationException($"Git repository '{Root}' has no commits.");
    public IReadOnlyList<GitChange> Changes => Repository.RetrieveStatus()
        .Select(entry => new GitChange(RelativePath.Parse(entry.FilePath), entry.State))
        .ToArray();
    public bool IsDirty => Changes.Count != 0;
    public TagCollection Tags => Repository.Tags;
    public Repository Repository { get; }

    public GitAdd Add { get; }
    public GitReset Reset { get; }
    public GitCommit Commit { get; }
    public GitPush Push { get; }
    public GitCreateTag CreateTag { get; }
    public GitPushTag PushTag { get; }

    public IReadOnlyList<Commit> CommitsSince(string baseBranch)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseBranch);
        return CommitsSince(Repository.Branches[baseBranch]
            ?? throw new KeyNotFoundException($"Git branch '{baseBranch}' was not found in '{Root}'."));
    }

    public IReadOnlyList<Commit> CommitsSince(Branch baseBranch)
    {
        ArgumentNullException.ThrowIfNull(baseBranch);
        var head = CurrentCommit;
        var baseCommit = baseBranch.Tip
            ?? throw new InvalidOperationException($"Git branch '{baseBranch.FriendlyName}' has no commits.");
        var mergeBase = Repository.ObjectDatabase.FindMergeBase(head, baseCommit)
            ?? throw new InvalidOperationException($"Git branch '{baseBranch.FriendlyName}' has no common history with HEAD.");
        var commits = new List<Commit>();

        for (var commit = head; commit.Id != mergeBase.Id; commit = commit.Parents.FirstOrDefault()
                 ?? throw new InvalidOperationException("The first-parent history ended before the merge base."))
            commits.Add(commit);

        return commits;
    }

    public ExecProcess Exec(string arguments, ExecOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return Do.Exec($"git -C {Root.QuotedArgument()} {arguments}", options);
    }

    internal string DefaultPushRemote
    {
        get
        {
            var branch = CurrentBranch;
            return ReadConfig(branch is null ? null : $"branch.{branch}.pushRemote")
                ?? ReadConfig("remote.pushDefault")
                ?? ReadConfig(branch is null ? null : $"branch.{branch}.remote")
                ?? "origin";
        }
    }

    string? ReadConfig(string? key) => key is null ? null : Repository.Config.Get<string>(key)?.Value;

    public void Dispose() => Repository.Dispose();
}

public sealed record GitChange(RelativePath Path, FileStatus State);

public sealed record GitAuthor(string Name, string Email);
