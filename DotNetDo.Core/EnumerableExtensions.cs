namespace DotNetDo;

/// <summary>Adds scripting-oriented enumerable helpers.</summary>
public static class EnumerableExtensions
{
    extension<T>(IEnumerable<T> enumerable)
    {
        /// <summary>Joins the string representation of each value using the supplied separator.</summary>
        public string JoinWith(string? separator) => string.Join(separator, enumerable);

        /// <summary>Joins the string representation of each value using the supplied separator.</summary>
        public string JoinWith(char separator) => string.Join(separator, enumerable);

        /// <summary>Joins the string representation of each value using the current environment's newline.</summary>
        public string JoinLines() => enumerable.JoinWith(Environment.NewLine);

        /// <summary>Returns whether the sequence contains no values.</summary>
        public bool None() => !enumerable.Any();

        /// <summary>Returns whether no sequence value satisfies the predicate.</summary>
        public bool None(Func<T, bool> predicate) => !enumerable.Any(predicate);

        /// <summary>Returns whether the source begins with the complete comparison sequence using the supplied comparer.</summary>
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
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable) where T : class => enumerable.OfType<T>();

    /// <summary>Returns the values held by nullable value types.</summary>
    public static IEnumerable<T> WhereHasValue<T>(this IEnumerable<T?> enumerable) where T : struct =>
        enumerable
            .Where(value => value.HasValue)
            .Select(value => value.GetValueOrDefault());
}
