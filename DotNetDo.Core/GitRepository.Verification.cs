using System.Text;
using LibGit2Sharp;
using Serilog;

namespace DotNetDo;

public sealed partial class GitRepository
{
    /// <summary>Runs a synchronous operation and asynchronously verifies that it preserved Git-visible repository state.</summary>
    /// <param name="operation">The operation guarded by repository snapshots.</param>
    /// <returns>A task that completes when verification succeeds.</returns>
    /// <exception cref="RepositoryChangedException">The operation completed but changed the repository.</exception>
    public Task VerifyUnchanged(Action operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return VerifyUnchanged(() =>
            {
                operation();
                return Task.CompletedTask;
            });
    }

    /// <summary>Runs an asynchronous operation and throws when it changes Git-visible repository state.</summary>
    /// <param name="operation">The operation guarded by repository snapshots.</param>
    /// <returns>A task that completes when verification succeeds.</returns>
    /// <exception cref="RepositoryChangedException">The operation completed but changed the repository.</exception>
    public async Task VerifyUnchanged(Func<Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var before = await RepositorySnapshot.Capture(this);

        await operation();

        var after = await RepositorySnapshot.Capture(this);
        var changes = RepositoryChanges.Compare(Repository, before, after);
        if (changes.ChangedPaths.Count == 0)
            return;

        Log.Error(
            "Repository changed during verification:{NewLine}{Details:l}",
            Environment.NewLine,
            changes.Format());
        throw new RepositoryChangedException(changes.ChangedPaths);
    }

    sealed class RepositorySnapshot
    {
        const SubmoduleStatus WorktreeSubmoduleStatus =
            SubmoduleStatus.WorkDirUninitialized
            | SubmoduleStatus.WorkDirAdded
            | SubmoduleStatus.WorkDirDeleted
            | SubmoduleStatus.WorkDirModified
            | SubmoduleStatus.WorkDirFilesIndexDirty
            | SubmoduleStatus.WorkDirFilesModified
            | SubmoduleStatus.WorkDirFilesUntracked;

        RepositorySnapshot(Tree index, Tree worktree, Dictionary<string, SubmoduleStatus> submodules)
        {
            Index = index;
            Worktree = worktree;
            Submodules = submodules;
        }

        public Tree Index { get; }
        public Tree Worktree { get; }
        public Dictionary<string, SubmoduleStatus> Submodules { get; }

        public static async Task<RepositorySnapshot> Capture(GitRepository repository)
        {
            var index = await WriteTree(repository, "write-tree", new ExecOptions { Log = ExecLog.None });
            var worktree = await WriteWorktree(repository, index);
            var submodules = repository.Repository.Submodules.ToDictionary(
                submodule => submodule.Path,
                submodule => submodule.RetrieveStatus() & WorktreeSubmoduleStatus,
                StringComparer.Ordinal);

            return new RepositorySnapshot(index, worktree, submodules);
        }

        static async Task<Tree> WriteWorktree(GitRepository repository, Tree index)
        {
            var temporaryIndex = Do.CreateTempFile("dotnetdo-git-index-");
            temporaryIndex.Delete();

            try
            {
                var add = repository.Add with
                    {
                        All = true,
                        Verbose = false,
                        Environment = env => env.SetItem("GIT_INDEX_FILE", temporaryIndex),
                        Log = ExecLog.None,
                    };

                await RunGit(repository, $"read-tree {index.Id.Sha}", add);
                await add;
                return await WriteTree(repository, "write-tree", add);
            }
            finally
            {
                if (File.Exists(temporaryIndex))
                    File.Delete(temporaryIndex);
            }
        }

        static async Task<Tree> WriteTree(GitRepository repository, string arguments, ExecOptions options)
        {
            var objectId = (await RunGit(repository, arguments, options)).ReadText().Trim();
            return repository.Repository.Lookup<Tree>(objectId)
                ?? throw new NotFoundException($"Git tree '{objectId}' was not found.");
        }

        static async Task<ExecResult> RunGit(GitRepository repository, string arguments, ExecOptions options) =>
            await repository.Exec(arguments, options);

    }

    sealed class RepositoryChanges
    {
        const int MaximumDiffLines = 12;
        static readonly CompareOptions CompareOptions = new()
            {
                ContextLines = 2,
                Similarity = SimilarityOptions.None,
            };

        readonly Dictionary<string, ChangeDetails> _changes;

        RepositoryChanges(Dictionary<string, ChangeDetails> changes)
        {
            _changes = changes;
            ChangedPaths = changes.Keys
                .OrderBy(path => path, StringComparer.Ordinal)
                .Select(RelativePath.Parse)
                .ToArray();
        }

        public IReadOnlyList<RelativePath> ChangedPaths { get; }

        public static RepositoryChanges Compare(
            Repository repository,
            RepositorySnapshot before,
            RepositorySnapshot after)
        {
            var changes = new Dictionary<string, ChangeDetails>(StringComparer.Ordinal);
            AddTreeChanges(repository, before.Worktree, after.Worktree, changes);
            AddTreeChanges(repository, before.Index, after.Index, changes);
            AddSubmoduleChanges(before.Submodules, after.Submodules, changes);
            return new RepositoryChanges(changes);
        }

        static void AddTreeChanges(
            Repository repository,
            Tree before,
            Tree after,
            Dictionary<string, ChangeDetails> changes)
        {
            using var patch = repository.Diff.Compare<Patch>(before, after, CompareOptions);

            foreach (var entry in patch)
            {
                changes.TryAdd(
                    entry.Path,
                    new ChangeDetails(
                        Marker(entry.Status),
                        entry.IsBinaryComparison
                            ? ["[binary content changed]"]
                            : DiffLines(entry.Patch)));
            }
        }

        static void AddSubmoduleChanges(
            Dictionary<string, SubmoduleStatus> before,
            Dictionary<string, SubmoduleStatus> after,
            Dictionary<string, ChangeDetails> changes)
        {
            foreach (var path in before.Keys.Union(after.Keys, StringComparer.Ordinal))
            {
                if (DictionaryValueEqual(before, after, path))
                    continue;

                changes.TryAdd(path, new ChangeDetails('M', ["[submodule state changed]"]));
            }
        }

        static bool DictionaryValueEqual<TKey, TValue>(
            Dictionary<TKey, TValue> before,
            Dictionary<TKey, TValue> after,
            TKey key
        ) where TKey : notnull =>
            before.TryGetValue(key, out var beforeValue)
            && after.TryGetValue(key, out var afterValue)
            && EqualityComparer<TValue>.Default.Equals(beforeValue, afterValue);

        static IReadOnlyList<string> DiffLines(string patch)
        {
            var lines = patch.SplitLines();
            var firstHunk = Array.FindIndex(lines, line => line.StartsWith("@@", StringComparison.Ordinal));
            if (firstHunk < 0)
                return ["[Git metadata changed]"];

            return lines
                .Skip(firstHunk + 1)
                .TakeWhile(line => !line.StartsWith("@@", StringComparison.Ordinal))
                .Where(IsDiffLine)
                .Take(MaximumDiffLines)
                .ToArray();
        }

        static bool IsDiffLine(string line) =>
            line.Length != 0 && line[0] is ' ' or '+' or '-';

        static char Marker(ChangeKind change) => change switch
        {
            ChangeKind.Added => 'A',
            ChangeKind.Deleted => 'D',
            _ => 'M',
        };

        public string Format()
        {
            var details = new StringBuilder();

            foreach (var path in ChangedPaths)
            {
                var change = _changes[path.UnixPath];
                details.Append(change.Marker).Append(' ').AppendLine(path.UnixPath);
                foreach (var line in change.Lines)
                    details.Append("  ").AppendLine(line);
            }

            return details.ToString().TrimEnd();
        }

        sealed record ChangeDetails(char Marker, IReadOnlyList<string> Lines);
    }
}

/// <summary>Reports paths whose Git-visible state changed during repository verification.</summary>
public sealed class RepositoryChangedException : Exception
{
    /// <summary>Creates an exception for the supplied changed paths.</summary>
    /// <param name="changedPaths">Repository-relative paths changed during verification.</param>
    public RepositoryChangedException(IReadOnlyList<RelativePath> changedPaths)
        : this(Normalize(changedPaths)) { }

    RepositoryChangedException(RelativePath[] changedPaths)
        : base(CreateMessage(changedPaths))
    {
        ChangedPaths = Array.AsReadOnly(changedPaths);
    }

    /// <summary>Every changed path, sorted by its repository-relative Unix representation.</summary>
    public IReadOnlyList<RelativePath> ChangedPaths { get; }

    static RelativePath[] Normalize(IReadOnlyList<RelativePath> changedPaths)
    {
        ArgumentNullException.ThrowIfNull(changedPaths);
        return changedPaths
            .Select(path => path ?? throw new ArgumentException("Changed paths cannot contain null.", nameof(changedPaths)))
            .OrderBy(path => path.UnixPath, StringComparer.Ordinal)
            .ToArray();
    }

    static string CreateMessage(IReadOnlyList<RelativePath> changedPaths) =>
        "Repository changed during verification:" + Environment.NewLine
        + string.Join(Environment.NewLine, changedPaths.Select(path => $"- {path.UnixPath}"));
}
