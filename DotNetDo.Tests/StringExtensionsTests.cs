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

    [Theory]
    [InlineData("HTTPServer2", "httpServer2", "HttpServer2", "http_server2", "HTTP_SERVER2", "http-server2", "HTTP-SERVER2")]
    [InlineData("version2Value", "version2Value", "Version2Value", "version2_value", "VERSION2_VALUE", "version2-value", "VERSION2-VALUE")]
    [InlineData("foo.bar/baz", "fooBarBaz", "FooBarBaz", "foo_bar_baz", "FOO_BAR_BAZ", "foo-bar-baz", "FOO-BAR-BAZ")]
    [InlineData("cafe\u0301-value", "caféValue", "CaféValue", "café_value", "CAFÉ_VALUE", "café-value", "CAFÉ-VALUE")]
    [InlineData("123 value", "123Value", "123Value", "123_value", "123_VALUE", "123-value", "123-VALUE")]
    [InlineData("!!!", "", "", "", "", "", "")]
    public void Converts_identifier_case(
        string value,
        string camel,
        string pascal,
        string snakeLower,
        string snakeUpper,
        string kebabLower,
        string kebabUpper
    )
    {
        Assert.Equal(camel, value.ToCamelCase());
        Assert.Equal(pascal, value.ToPascalCase());
        Assert.Equal(snakeLower, value.ToSnakeCaseLower());
        Assert.Equal(snakeUpper, value.ToSnakeCaseUpper());
        Assert.Equal(kebabLower, value.ToKebabCaseLower());
        Assert.Equal(kebabUpper, value.ToKebabCaseUpper());
    }

    [Fact]
    public void Casing_helpers_reject_null()
    {
        string value = null!;

        Assert.Throws<ArgumentNullException>(() => value.ToCamelCase());
        Assert.Throws<ArgumentNullException>(() => value.ToPascalCase());
        Assert.Throws<ArgumentNullException>(() => value.ToSnakeCaseLower());
        Assert.Throws<ArgumentNullException>(() => value.ToSnakeCaseUpper());
        Assert.Throws<ArgumentNullException>(() => value.ToKebabCaseLower());
        Assert.Throws<ArgumentNullException>(() => value.ToKebabCaseUpper());
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
    public void Quotes_parameter_values_for_command_interpolation()
    {
        var parameter = Do.Param($"parameter-{Guid.NewGuid()}", "some value");
        var number = Do.Param($"number-{Guid.NewGuid()}", 12.5m);
        var secret = Do.Secret($"secret-{Guid.NewGuid()}", "secret value");

        Assert.Equal("\"some value\"", parameter.QuotedArgument());
        Assert.Equal("\"some value\"", parameter.Required().QuotedArgument());
        Assert.Equal("12.5", number.QuotedArgument());
        Assert.Equal("\"secret value\"", secret.QuotedArgument());
        Assert.Equal("\"secret value\"", secret.Required().QuotedArgument());
    }

    [Fact]
    public void Missing_optional_parameters_render_as_null_arguments()
    {
        var parameter = Do.Param($"parameter-{Guid.NewGuid()}");
        var secret = Do.Secret($"secret-{Guid.NewGuid()}");

        Assert.Null(parameter.QuotedArgument());
        Assert.Null(secret.QuotedArgument());
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
