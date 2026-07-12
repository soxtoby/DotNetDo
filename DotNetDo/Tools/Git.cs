using LibGit2Sharp;

namespace DotNetDo;

public static partial class Tools
{
    public static class Git
    {
        public static GitAdd Add => new();
        public static GitReset Reset => new();
        public static GitCommit Commit => new();
        public static GitPush Push => new();
        public static GitCreateTag CreateTag => new();
        public static GitPushTag PushTag => new();
    }
}

public abstract record GitCommand : ToolCommand
{
    readonly GitRepository? _repository;

    protected GitCommand() { }
    protected GitCommand(GitRepository repository) => _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    protected GitCommand(GitCommand original) : base(original) => _repository = original._repository;

    protected GitRepository Repository => _repository ?? Do.GitRepo;
    protected string GitPrefix => $"git -C {Repository.Root.QuotedArgument()}";

    protected string RenderPaths(IReadOnlyList<RelativePath> paths, bool all, string allArguments)
    {
        ArgumentNullException.ThrowIfNull(paths);
        
        if (all == (paths.Count != 0))
            throw new InvalidOperationException("Specify either Paths or All, but not both.");

        if (all)
            return allArguments;

        return "-- " + string.Join(" ", paths.Select(RenderPath));
    }

    string RenderPath(RelativePath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        _ = Repository.Root / path;
        return path.UnixPath.QuotedArgument();
    }
}

public sealed record GitAdd : GitCommand
{
    internal GitAdd() { }
    internal GitAdd(GitRepository repository) : base(repository) { }

    public IReadOnlyList<RelativePath> Paths { get; init; } = [];
    public bool All { get; init; }

    protected override string CommandPrefix => $"{GitPrefix} add {RenderPaths(Paths, All, "--all")}";
}

public sealed record GitReset : GitCommand
{
    internal GitReset() { }
    internal GitReset(GitRepository repository) : base(repository) { }

    public IReadOnlyList<RelativePath> Paths { get; init; } = [];
    public bool All { get; init; }

    protected override string CommandPrefix => $"{GitPrefix} reset {RenderPaths(Paths, All, "-- .")}";
}

public sealed record GitCommit : GitCommand
{
    internal GitCommit() { }
    internal GitCommit(GitRepository repository) : base(repository) { }

    public string? Message { get; init; }
    public bool All { get; init; }
    public GitAuthor? Author { get; init; }

    protected override string CommandPrefix
    {
        get
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Message);
            var all = All ? " --all" : "";
            var author = Author is null ? "" : $" --author {RenderAuthor(Author)}";
            return $"{GitPrefix} commit --message {Message.QuotedArgument()}{all}{author}";
        }
    }

    static string RenderAuthor(GitAuthor author)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(author.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(author.Email);
        return $"{author.Name} <{author.Email}>".QuotedArgument();
    }
}

public sealed record GitPush : GitCommand
{
    internal GitPush() { }
    internal GitPush(GitRepository repository) : base(repository) { }

    public string? Remote { get; init; }

    protected override string CommandPrefix => string.IsNullOrWhiteSpace(Remote)
        ? $"{GitPrefix} push"
        : $"{GitPrefix} push {Remote.QuotedArgument()}";
}

public sealed record GitCreateTag : GitCommand
{
    internal GitCreateTag() { }
    internal GitCreateTag(GitRepository repository) : base(repository) { }

    public string? Name { get; init; }
    public string? Message { get; init; }
    public Commit? Target { get; init; }

    protected override string CommandPrefix
    {
        get
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Name);
            ArgumentException.ThrowIfNullOrWhiteSpace(Message);
            var target = Target ?? Repository.CurrentCommit;
            return $"{GitPrefix} tag --annotate {Name.QuotedArgument()} --message {Message.QuotedArgument()} {target.Sha.QuotedArgument()}";
        }
    }
}

public sealed record GitPushTag : GitCommand
{
    internal GitPushTag() { }
    internal GitPushTag(GitRepository repository) : base(repository) { }

    public Tag? Tag { get; init; }
    public string? Remote { get; init; }

    protected override string CommandPrefix
    {
        get
        {
            ArgumentNullException.ThrowIfNull(Tag);
            var remote = string.IsNullOrWhiteSpace(Remote) ? Repository.DefaultPushRemote : Remote;
            return $"{GitPrefix} push {remote.QuotedArgument()} tag {Tag.FriendlyName.QuotedArgument()}";
        }
    }
}
