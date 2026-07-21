using Xunit;

namespace DotNetDo.Tests;

public sealed class EnumerableExtensionsTests
{
    [Fact]
    public void Joins_value_strings()
    {
        Assert.Equal("1, 2, 3", new[] { 1, 2, 3 }.JoinWith(", "));
        Assert.Equal("1,2,3", new[] { 1, 2, 3 }.JoinWith(','));
        Assert.Equal($"1{Environment.NewLine}2", new[] { 1, 2 }.JoinLines());
        Assert.Equal(string.Empty, Array.Empty<int>().JoinLines());
    }

    [Fact]
    public void Detects_empty_and_non_matching_sequences()
    {
        Assert.True(Array.Empty<int>().None());
        Assert.False(new[] { 1 }.None());
        Assert.True(new[] { 1, 2 }.None(value => value > 2));
        Assert.False(new[] { 1, 2 }.None(value => value == 2));
    }

    [Fact]
    public void Filters_and_unwraps_missing_values()
    {
        string?[] references = ["one", null, "two"];
        int?[] values = [1, null, 2];

        Assert.Equal(["one", "two"], references.WhereNotNull());
        Assert.Equal([1, 2], values.WhereHasValue());
    }

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
