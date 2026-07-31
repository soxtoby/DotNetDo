using Xunit;

namespace DotNetDo.Tests;

public sealed class PathValueTests
{
    [Fact]
    public void Absolute_root_has_no_parent()
    {
        Assert.Throws<InvalidOperationException>(() => AbsolutePath.Parse("/").Parent);
        Assert.Equal(AbsolutePath.Parse("/"), AbsolutePath.Parse("/a").Parent);
    }

    [Theory]
    [InlineData("a/b", "a")]
    [InlineData("a", ".")]
    [InlineData(".", "..")]
    [InlineData("..", "../..")]
    [InlineData("../a", "..")]
    public void Relative_parent_extends_parent_traversal(string path, string expected)
    {
        Assert.Equal(RelativePath.Parse(expected), RelativePath.Parse(path).Parent);
    }
}
