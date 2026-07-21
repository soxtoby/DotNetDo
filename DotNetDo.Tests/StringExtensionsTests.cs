using System.Text.RegularExpressions;
using Xunit;

namespace DotNetDo.Tests;

public sealed class StringExtensionsTests
{
    [Fact]
    public void Splits_mixed_line_endings_without_a_terminal_empty_line()
    {
        Assert.Equal(["one", "two", "", "three"], "one\r\ntwo\n\rthree\r".SplitLines());
        Assert.Empty(string.Empty.SplitLines());
    }

    [Fact]
    public void Detects_null_empty_and_whitespace_strings()
    {
        string? missing = null;

        Assert.True(missing.IsNullOrEmpty());
        Assert.True(string.Empty.IsNullOrEmpty());
        Assert.False(" ".IsNullOrEmpty());
        Assert.True(missing.IsNullOrWhiteSpace());
        Assert.True(" \t".IsNullOrWhiteSpace());
        Assert.False("value".IsNullOrWhiteSpace());
    }

    [Fact]
    public void Quotes_path_values_for_command_interpolation()
    {
        var absolute = AbsolutePath.Parse(OperatingSystem.IsWindows() ? @"C:\directory\file name.txt" : "/directory/file name.txt");
        var relative = RelativePath.Parse("directory/file name.txt");

        Assert.Equal(absolute.ToString().QuotedArgument(), absolute.QuotedArgument());
        Assert.Equal(relative.ToString().QuotedArgument(), relative.QuotedArgument());
    }

    [Fact]
    public void Matches_regular_expressions()
    {
        const string input = "Version: 1.2\nVersion: 3.4";
        const string pattern = @"^Version: (?<version>\d+\.\d+)$";

        Assert.True(input.IsRegexMatch(pattern, RegexOptions.Multiline));
        Assert.Equal("1.2", input.RegexMatch(pattern, RegexOptions.Multiline).Groups["version"].Value);
        Assert.Equal(2, input.RegexMatches(pattern, RegexOptions.Multiline).Count);
    }

    [Fact]
    public void Replaces_regular_expression_matches()
    {
        Assert.Equal("v1 v2", "1 2".RegexReplace(@"(\d)", "v$1"));
        Assert.Equal("ONE TWO", "one two".RegexReplace(@"\w+", match => match.Value.ToUpperInvariant()));
    }

    [Fact]
    public void Splits_at_regular_expression_matches()
    {
        Assert.Equal(["one", "two", "three"], "one, two ;three".RegexSplit(@"\s*[,;]\s*"));
    }

    [Fact]
    public void Passes_explicit_regular_expression_timeouts()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => "value".IsRegexMatch("value", timeout: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => "value".RegexMatch("value", timeout: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => "value".RegexMatches("value", timeout: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => "value".RegexReplace("value", "other", timeout: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => "value".RegexReplace("value", _ => "other", timeout: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => "value".RegexSplit("value", timeout: TimeSpan.Zero));
    }
}
