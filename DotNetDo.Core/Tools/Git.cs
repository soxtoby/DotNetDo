using LibGit2Sharp;
using Serilog.Events;

namespace DotNetDo;

public static partial class Tools
{
    /// <summary>Stages, unstages, commits, pushes, and tags the discovered workspace repository.</summary>
    public static class Git
    {
        /// <summary>Stages selected or all working-tree changes.</summary>
        public static GitAdd Add => new();
        /// <summary>Unstages selected or all index changes without changing working-tree files.</summary>
        public static GitReset Reset => new();
        /// <summary>Records staged changes as a new commit.</summary>
        public static GitCommit Commit => new();
        /// <summary>Pushes the current branch to its configured or selected remote.</summary>
        public static GitPush Push => new();
        /// <summary>Creates an annotated tag at HEAD or a selected commit.</summary>
        public static GitCreateTag CreateTag => new();
        /// <summary>Pushes one selected tag to the configured or selected remote.</summary>
        public static GitPushTag PushTag => new();
    }
}

/// <summary>Runs Git against an explicitly bound repository, or the discovered workspace repository.</summary>
public abstract record GitCommand : ExecToolCommand
{
    readonly GitRepository? _repository;

    /// <summary>Initializes a Git command, optionally bound to a repository working directory.</summary>
    protected GitCommand() { }
    /// <summary>Initializes a Git command, optionally bound to a repository working directory.</summary>
    protected GitCommand(GitRepository repository) => _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    /// <summary>Initializes a Git command, optionally bound to a repository working directory.</summary>
    protected GitCommand(GitCommand original) : base(original) => _repository = original._repository;

    /// <summary>The bound repository, falling back to the workspace repository when none was supplied.</summary>
    protected GitRepository Repository => _repository ?? Do.GitRepo;

    /// <summary>Targets Git at the repository root without changing the process working directory.</summary>
    protected string GitPrefix => $"git -C {Repository.Root.QuotedArgument()}";

    /// <summary>Renders either explicit repository-relative paths or the command's all-path form, rejecting ambiguous selection.</summary>
    protected static string RenderPaths(IReadOnlyList<RelativePath> paths, bool all, string allArguments)
    {
        ArgumentNullException.ThrowIfNull(paths);

        if (all == (paths.Count != 0))
            throw new InvalidOperationException("Specify either Paths or All, but not both.");

        if (all)
            return allArguments;

        return "-- " + string.Join(" ", paths.Select(RenderPath));
    }

    static string RenderPath(RelativePath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.UnixPath.QuotedArgument();
    }
}

/// <summary>Stages selected paths or all working-tree changes in the repository index.</summary>
public sealed record GitAdd : GitCommand
{
    internal GitAdd() => Verbose = GitOutputVolume.From(Logging.Level).Verbose;
    internal GitAdd(GitRepository repository) : base(repository) => Verbose = GitOutputVolume.From(Logging.Level).Verbose;

    /// <summary>Repository-relative paths to stage; cannot be combined with <see cref="All"/>.</summary>
    public IReadOnlyList<RelativePath> Paths { get; init => field = value.ToArray(); } = [];
    /// <summary>Stages tracked, modified, deleted, and untracked paths; cannot be combined with <see cref="Paths"/>.</summary>
    public bool All { get; init; }
    /// <summary>Whether Git reports each added path.</summary>
    public bool Verbose { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            $"{GitPrefix} add",
            Arg("--verbose", Verbose),
            RenderPaths(Paths, All, "--all")
        ];
}

/// <summary>Resets selected or all index entries to HEAD without changing working-tree files.</summary>
public sealed record GitReset : GitCommand
{
    internal GitReset() => Quiet = GitOutputVolume.From(Logging.Level).Quiet;
    internal GitReset(GitRepository repository) : base(repository) => Quiet = GitOutputVolume.From(Logging.Level).Quiet;

    /// <summary>Repository-relative paths to unstage; cannot be combined with <see cref="All"/>.</summary>
    public IReadOnlyList<RelativePath> Paths { get; init => field = value.ToArray(); } = [];
    /// <summary>Unstages every path beneath the repository root; cannot be combined with <see cref="Paths"/>.</summary>
    public bool All { get; init; }
    /// <summary>Whether Git reports only errors.</summary>
    public bool Quiet { get; init; }
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            $"{GitPrefix} reset",
            Arg("--quiet", Quiet),
            RenderPaths(Paths, All, "-- .")
        ];
}

/// <summary>Creates a commit from staged changes, optionally staging tracked modifications first.</summary>
public sealed record GitCommit : GitCommand
{
    internal GitCommit() => Quiet = GitOutputVolume.From(Logging.Level).Quiet;

    internal GitCommit(GitRepository repository)
        : base(repository) => Quiet = GitOutputVolume.From(Logging.Level).Quiet;

    /// <summary>The required non-empty commit message.</summary>
    public string? Message { get; init; }
    /// <summary>Stages modifications and deletions to tracked files before committing; untracked files remain unstaged.</summary>
    public bool All { get; init; }

    /// <summary>The author identity used for the commit.</summary>
    public GitAuthor? Author { get; init; }

    /// <summary>Whether Git suppresses the successful commit summary.</summary>
    public bool Quiet { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Message);
            return
                [
                    $"{GitPrefix} commit",
                    Arg("--quiet", Quiet),
                    Arg("--message", Message),
                    Arg("--all", All),
                    Arg("--author", Author is null ? null : RenderAuthor(Author)),
                ];
        }
    }

    static string RenderAuthor(GitAuthor author)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(author.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(author.Email);
        return $"{author.Name} <{author.Email}>";
    }
}

/// <summary>Pushes the current branch using Git's configured refspec and upstream behavior.</summary>
public sealed record GitPush : GitCommand
{
    internal GitPush()
    {
        (Quiet, Verbose) = GitOutputVolume.From(Logging.Level);
    }

    internal GitPush(GitRepository repository)
        : base(repository)
    {
        (Quiet, Verbose) = GitOutputVolume.From(Logging.Level);
    }

    /// <summary>The remote name; when omitted, Git uses its configured default.</summary>
    public string? Remote { get; init; }
    /// <summary>Whether Git reduces reported push details.</summary>
    public bool Quiet { get; init; }
    /// <summary>Whether Git reports additional push details.</summary>
    public bool Verbose { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            $"{GitPrefix} push",
            Arg("--quiet", Quiet),
            Arg("--verbose", Verbose),
            Arg(Remote)
        ];
}

/// <summary>Creates an annotated tag with a required name and message.</summary>
public sealed record GitCreateTag : GitCommand
{
    internal GitCreateTag() { }

    internal GitCreateTag(GitRepository repository)
        : base(repository) { }

    /// <summary>The required non-empty tag name accepted by Git.</summary>
    public string? Name { get; init; }
    /// <summary>The required non-empty annotation message.</summary>
    public string? Message { get; init; }
    /// <summary>The commit tagged; when omitted, Git tags HEAD.</summary>
    public Commit? Target { get; init; }


    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Name);
            ArgumentException.ThrowIfNullOrWhiteSpace(Message);
            var target = Target ?? Repository.CurrentCommit;
            return
                [
                    $"{GitPrefix} tag",
                    "--annotate",
                    Arg(Name),
                    Arg("--message", Message),
                    Arg(target.Sha),
                ];
        }
    }
}

/// <summary>Pushes one existing tag without pushing branches or other tags.</summary>
public sealed record GitPushTag : GitCommand
{
    internal GitPushTag()
    {
        (Quiet, Verbose) = GitOutputVolume.From(Logging.Level);
    }

    internal GitPushTag(GitRepository repository)
        : base(repository)
    {
        (Quiet, Verbose) = GitOutputVolume.From(Logging.Level);
    }

    /// <summary>The required repository tag to push.</summary>
    public Tag? Tag { get; init; }
    /// <summary>The remote name; when omitted, Git uses its configured default.</summary>
    public string? Remote { get; init; }
    /// <summary>Whether Git reduces reported push details.</summary>
    public bool Quiet { get; init; }
    /// <summary>Whether Git reports additional push details.</summary>
    public bool Verbose { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            ArgumentNullException.ThrowIfNull(Tag);
            return
                [
                    $"{GitPrefix} push",
                    Arg("--quiet", Quiet),
                    Arg("--verbose", Verbose),
                    Arg(string.IsNullOrWhiteSpace(Remote) ? Repository.DefaultPushRemote : Remote),
                    "tag",
                    Arg(Tag.FriendlyName),
                ];
        }
    }
}

static class GitOutputVolume
{
    public static (bool Quiet, bool Verbose) From(LogEventLevel level)
    {
        return (level >= LogEventLevel.Warning, level <= LogEventLevel.Debug);
    }
}
