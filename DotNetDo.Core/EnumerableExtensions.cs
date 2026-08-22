namespace DotNetDo;

/// <summary>Adds scripting-oriented enumerable helpers.</summary>
public static class EnumerableExtensions
{
    extension(IReadOnlyCollection<AbsolutePath> paths)
    {
        /// <summary>Deletes the supplied files and directories, omitting duplicates and paths beneath another supplied path.</summary>
        public void DeleteAll()
        {
            ArgumentNullException.ThrowIfNull(paths);

            var snapshot = paths.ToArray();
            if (snapshot.Any(path => path is null))
                throw new ArgumentException("The collection cannot contain null paths.", nameof(paths));
            var root = snapshot.FirstOrDefault(path => path.IsRoot);
            if (root is not null)
                throw new InvalidOperationException($"Cannot delete the root directory '{root}'.");

            foreach (var path in snapshot
                .Distinct()
                .Where(path => snapshot.None(other => other != path && path.IsWithin(other))))
            {
                path.Delete();
            }
        }

        /// <summary>Recreates the supplied directories, processing ancestors before descendants.</summary>
        public void RecreateAll()
        {
            ArgumentNullException.ThrowIfNull(paths);

            var snapshot = paths.ToArray();
            if (snapshot.Any(path => path is null))
                throw new ArgumentException("The collection cannot contain null paths.", nameof(paths));
            var root = snapshot.FirstOrDefault(path => path.IsRoot);
            if (root is not null)
                throw new InvalidOperationException($"Cannot recreate the root directory '{root}'.");

            foreach (var path in snapshot
                .Distinct()
                .OrderBy(path => path.GetAncestry().Count()))
            {
                path.RecreateDirectory();
            }
        }
    }

    extension<T>(IEnumerable<T> enumerable)
    {
        /// <summary>Joins the string representation of each value using the supplied separator.</summary>
        /// <param name="separator">Text inserted between adjacent values; <see langword="null"/> inserts nothing.</param>
        public string JoinWith(string? separator) => string.Join(separator, enumerable);

        /// <summary>Joins the string representation of each value using the supplied separator.</summary>
        /// <param name="separator">The character inserted between adjacent values.</param>
        public string JoinWith(char separator) => string.Join(separator, enumerable);

        /// <summary>Joins the string representation of each value using the current environment's newline.</summary>
        public string JoinLines() => enumerable.JoinWith(Environment.NewLine);

        /// <summary>Returns whether the sequence contains no values.</summary>
        public bool None() => !enumerable.Any();

        /// <summary>Returns whether no sequence value satisfies the predicate.</summary>
        /// <param name="predicate">The condition tested against sequence values until one matches.</param>
        public bool None(Func<T, bool> predicate) => !enumerable.Any(predicate);

        /// <summary>Returns whether the source begins with the complete comparison sequence using the supplied comparer.</summary>
        /// <param name="other">The prefix sequence; an empty sequence always matches.</param>
        /// <param name="comparer">The equality comparer; <see langword="null"/> uses <see cref="EqualityComparer{T}.Default"/>.</param>
        public bool SequenceStartsWith(IEnumerable<T> other, IEqualityComparer<T>? comparer = null)
        {
            ArgumentNullException.ThrowIfNull(enumerable);
            ArgumentNullException.ThrowIfNull(other);

            comparer ??= EqualityComparer<T>.Default;

            using var source = enumerable.GetEnumerator();
            return other.All(item => source.MoveNext() && comparer.Equals(source.Current, item));
        }
    }

    /// <summary>Returns only non-null values with nullable reference annotations removed.</summary>
    /// <param name="enumerable">The source sequence; enumeration remains deferred.</param>
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable) where T : class => enumerable.OfType<T>();

    /// <summary>Returns the values held by nullable value types.</summary>
    /// <param name="enumerable">The source sequence; enumeration remains deferred.</param>
    public static IEnumerable<T> WhereHasValue<T>(this IEnumerable<T?> enumerable) where T : struct =>
        enumerable
            .Where(value => value.HasValue)
            .Select(value => value.GetValueOrDefault());
}
