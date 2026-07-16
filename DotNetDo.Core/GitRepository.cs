using LibGit2Sharp;

namespace DotNetDo;

public static partial class Do
{
    /// <summary>Exposes the configured value or operation to script authors.</summary>
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

/// <summary>Owns a LibGit2Sharp repository and exposes bound Git command definitions.</summary>
public sealed class GitRepository : IDisposable
{
    /// <summary>Opens the repository containing the supplied directory and binds command values to its root.</summary>
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

    /// <summary>The root directory or root component represented by this value.</summary>
    public AbsolutePath Root { get; }
    /// <summary>The checked-out branch name, or <see langword="null"/> for a detached HEAD.</summary>
    public string? CurrentBranch => Repository.Info.IsHeadDetached ? null : Repository.Head.FriendlyName;
    /// <summary>The commit currently referenced by HEAD.</summary>
    public Commit CurrentCommit => Repository.Head.Tip
        ?? throw new InvalidOperationException($"Git repository '{Root}' has no commits.");
    /// <summary>Reads a fresh repository status snapshot and converts paths to normalized relative paths.</summary>
    public IReadOnlyList<GitChange> Changes => Repository.RetrieveStatus()
        .Select(entry => new GitChange(RelativePath.Parse(entry.FilePath), entry.State))
        .ToArray();
    /// <summary>Whether the repository currently has tracked or untracked changes.</summary>
    public bool IsDirty => Changes.Any(change => change.State != FileStatus.Ignored);
    /// <summary>The repository's live tag collection.</summary>
    public TagCollection Tags => Repository.Tags;
    /// <summary>The underlying LibGit2Sharp repository; owned and disposed by this wrapper.</summary>
    public Repository Repository { get; }

    /// <summary>Gets or sets add.</summary>
    public GitAdd Add { get; }
    /// <summary>Gets or sets reset.</summary>
    public GitReset Reset { get; }
    /// <summary>Gets or sets commit.</summary>
    public GitCommit Commit { get; }
    /// <summary>Gets or sets push.</summary>
    public GitPush Push { get; }
    /// <summary>Gets or sets create tag.</summary>
    public GitCreateTag CreateTag { get; }
    /// <summary>Gets or sets push tag.</summary>
    public GitPushTag PushTag { get; }

    /// <summary>Walks commits from HEAD back to, but excluding, the merge base with the supplied branch.</summary>
    public IReadOnlyList<Commit> CommitsSince(string baseBranch)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseBranch);
        return CommitsSince(Repository.Branches[baseBranch]
            ?? throw new KeyNotFoundException($"Git branch '{baseBranch}' was not found in '{Root}'."));
    }

    /// <summary>Walks commits from HEAD back to, but excluding, the merge base with the supplied branch.</summary>
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

    /// <summary>Starts the rendered command directly, without a shell, in the configured working directory.</summary>
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

    /// <summary>Releases resources owned by this wrapper.</summary>
    public void Dispose() => Repository.Dispose();
}

/// <summary>A repository-relative path and its current Git status.</summary>
public sealed record GitChange(RelativePath Path, FileStatus State);

/// <summary>A Git author name and email address.</summary>
public sealed record GitAuthor(string Name, string Email);
