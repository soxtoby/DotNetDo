using LibGit2Sharp;
using Serilog.Events;

namespace DotNetDo;

public static partial class Tools
{
    /// <summary>Provides fresh definitions for supported Git commands.</summary>
    public static class Git
    {
        /// <summary>Returns a fresh value so later <c>with</c> customization cannot affect other callers.</summary>
        public static GitAdd Add => new();
        /// <summary>Returns a fresh value so later <c>with</c> customization cannot affect other callers.</summary>
        public static GitReset Reset => new();
        /// <summary>Returns a fresh value so later <c>with</c> customization cannot affect other callers.</summary>
        public static GitCommit Commit => new();
        /// <summary>Returns a fresh value so later <c>with</c> customization cannot affect other callers.</summary>
        public static GitPush Push => new();
        /// <summary>Returns a fresh value so later <c>with</c> customization cannot affect other callers.</summary>
        public static GitCreateTag CreateTag => new();
        /// <summary>Returns a fresh value so later <c>with</c> customization cannot affect other callers.</summary>
        public static GitPushTag PushTag => new();
    }
}

/// <summary>Models the <c>GitCommand</c> command and its typed options.</summary>
public abstract record GitCommand : ExecToolCommand
{
    readonly GitRepository? _repository;

    /// <summary>Initializes a Git command, optionally bound to a repository working directory.</summary>
    protected GitCommand() { }
    /// <summary>Initializes a Git command, optionally bound to a repository working directory.</summary>
    protected GitCommand(GitRepository repository) => _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    /// <summary>Initializes a Git command, optionally bound to a repository working directory.</summary>
    protected GitCommand(GitCommand original) : base(original) => _repository = original._repository;

    /// <summary>The underlying LibGit2Sharp repository; owned and disposed by this wrapper.</summary>
    protected GitRepository Repository => _repository ?? Do.GitRepo;

    /// <summary>Renders the value as one quoted command-line argument.</summary>
    protected string GitPrefix => $"git -C {Repository.Root.QuotedArgument()}";

    /// <summary>Render paths.</summary>
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

/// <summary>Models the <c>GitAdd</c> command and its typed options.</summary>
public sealed record GitAdd : GitCommand
{
    internal GitAdd() => Verbose = GitOutputVolume.From(Logging.Level).Verbose;
    internal GitAdd(GitRepository repository) : base(repository) => Verbose = GitOutputVolume.From(Logging.Level).Verbose;

    /// <summary>Paths passed to the Git command.</summary>
    public IReadOnlyList<RelativePath> Paths { get; init => field = value.ToArray(); } = [];
    /// <summary>Whether the command should operate on all applicable paths or changes.</summary>
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

/// <summary>Models the <c>GitReset</c> command and its typed options.</summary>
public sealed record GitReset : GitCommand
{
    internal GitReset() => Quiet = GitOutputVolume.From(Logging.Level).Quiet;
    internal GitReset(GitRepository repository) : base(repository) => Quiet = GitOutputVolume.From(Logging.Level).Quiet;

    /// <summary>Paths passed to the Git command.</summary>
    public IReadOnlyList<RelativePath> Paths { get; init => field = value.ToArray(); } = [];
    /// <summary>Whether the command should operate on all applicable paths or changes.</summary>
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

/// <summary>Models the <c>GitCommit</c> command and its typed options.</summary>
public sealed record GitCommit : GitCommand
{
    internal GitCommit() => Quiet = GitOutputVolume.From(Logging.Level).Quiet;

    internal GitCommit(GitRepository repository)
        : base(repository) => Quiet = GitOutputVolume.From(Logging.Level).Quiet;

    /// <summary>The message passed to the command.</summary>
    public string? Message { get; init; }
    /// <summary>Whether the command should operate on all applicable paths or changes.</summary>
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

/// <summary>Models the <c>GitPush</c> command and its typed options.</summary>
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

/// <summary>Models the <c>GitCreateTag</c> command and its typed options.</summary>
public sealed record GitCreateTag : GitCommand
{
    internal GitCreateTag() { }

    internal GitCreateTag(GitRepository repository)
        : base(repository) { }

    /// <summary>The final path component, or <see langword="null"/> for a root or empty path.</summary>
    public string? Name { get; init; }
    /// <summary>The message passed to the command.</summary>
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

/// <summary>Models the <c>GitPushTag</c> command and its typed options.</summary>
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

    /// <summary>The tag to push.</summary>
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
