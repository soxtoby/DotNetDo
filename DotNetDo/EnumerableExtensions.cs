namespace DotNetDo;

public static class EnumerableExtensions
{
    public static bool SequenceStartsWith<T>(this IEnumerable<T> enumerable, IEnumerable<T> other, IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(enumerable);
        ArgumentNullException.ThrowIfNull(other);
        
        comparer ??= EqualityComparer<T>.Default;

        using var source = enumerable.GetEnumerator();
        return other.All(item => source.MoveNext() && comparer.Equals(source.Current, item));
    }
}
