namespace DotNetDo;

/// <summary>Adds sequence-prefix comparison helpers.</summary>
public static class EnumerableExtensions
{
    /// <summary>Returns whether the source begins with the complete comparison sequence using the supplied comparer.</summary>
    public static bool SequenceStartsWith<T>(this IEnumerable<T> enumerable, IEnumerable<T> other, IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(enumerable);
        ArgumentNullException.ThrowIfNull(other);
        
        comparer ??= EqualityComparer<T>.Default;

        using var source = enumerable.GetEnumerator();
        return other.All(item => source.MoveNext() && comparer.Equals(source.Current, item));
    }
}
