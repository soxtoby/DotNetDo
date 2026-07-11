using Xunit;

namespace DotNetDo.Tests;

public sealed class EnumerableExtensionsTests
{
    [Fact]
    public void Detects_sequence_prefixes()
    {
        Assert.True(new[] { 1, 2, 3 }.SequenceStartsWith([1, 2]));
        Assert.True(new[] { 1, 2, 3 }.SequenceStartsWith([]));
        Assert.False(new[] { 1, 2 }.SequenceStartsWith([1, 2, 3]));
        Assert.False(new[] { 1, 2, 3 }.SequenceStartsWith([1, 3]));
    }

    [Fact]
    public void Uses_the_supplied_comparer()
    {
        Assert.True(new[] { "A", "b" }.SequenceStartsWith(["a"], StringComparer.OrdinalIgnoreCase));
    }
}
