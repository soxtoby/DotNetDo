using System.Text;
using Xunit;

namespace DotNetDo.Tests;

public sealed class DotEnvTests : IDisposable
{
    readonly AbsolutePath _file = Do.CreateTempFile("dotnetdo-", ".env");

    public void Dispose() => _file.Delete();

    [Fact]
    public void Read_dot_env_returns_assignments_in_file_order()
    {
        _file.WriteText("""
            # Local development settings

            NODE_ENV=production
            export PORT=8080
              SPACED = trimmed value
            URL=https://example.com/?a=1&b=2 # inline comment
            CHANNEL=#general
            EMPTY=
            """);

        var result = _file.ReadDotEnv();

        Assert.Equal<KeyValuePair<string, string>>(
            [
                new("NODE_ENV", "production"),
                new("PORT", "8080"),
                new("SPACED", "trimmed value"),
                new("URL", "https://example.com/?a=1&b=2"),
                new("CHANNEL", "#general"),
                new("EMPTY", ""),
            ],
            result.ToArray());
    }

    [Fact]
    public void Read_dot_env_reads_quoted_values()
    {
        _file.WriteText(
            "SINGLE='literal \\n # not a comment' # comment\r\n"
            + "DOUBLE=\"tab\\tquote\\\" backslash\\\\ newline\\n unknown\\q\"\r\n"
            + "SINGLE_LINES='first\r\nsecond'\r\n"
            + "DOUBLE_LINES=\"first\r\nsecond\"\r\n"
            + "QUOTES=\"it's\"# comment\r\n");

        var result = _file.ReadDotEnv();

        Assert.Equal<KeyValuePair<string, string>>(
            [
                new("SINGLE", "literal \\n # not a comment"),
                new("DOUBLE", "tab\tquote\" backslash\\ newline\n unknown\\q"),
                new("SINGLE_LINES", "first\nsecond"),
                new("DOUBLE_LINES", "first\nsecond"),
                new("QUOTES", "it's"),
            ],
            result.ToArray());
    }

    [Fact]
    public void Read_dot_env_keeps_variable_references_unexpanded()
    {
        _file.WriteText("""
            HOME_COPY=$HOME
            PATH_COPY="${PATH}:/opt/tools"
            """);

        var result = _file.ReadDotEnv();

        Assert.Equal("$HOME", result["HOME_COPY"]);
        Assert.Equal("${PATH}:/opt/tools", result["PATH_COPY"]);
    }

    [Fact]
    public void Read_dot_env_with_a_repeated_name_uses_the_last_value()
    {
        _file.WriteText("""
            MODE=first
            OTHER=value
            MODE=second
            """);

        var result = _file.ReadDotEnv();

        Assert.Equal<KeyValuePair<string, string>>([new("MODE", "second"), new("OTHER", "value")], result.ToArray());
    }

    [Fact(Timeout = 10_000)]
    public async Task Read_dot_env_with_long_whitespace_runs_completes_promptly()
    {
        var spaces = new string(' ', 50_000);
        _file.WriteText($"VALUE=x{spaces}y\nCOMMENTED=x{spaces}# comment\n");

        var result = await Task.Run(_file.ReadDotEnv, TestContext.Current.CancellationToken);

        Assert.Equal($"x{spaces}y", result["VALUE"]);
        Assert.Equal("x", result["COMMENTED"]);
    }

    [Theory]
    [InlineData("A=1\nnot an assignment\n", 2)]
    [InlineData("A=1\nMY KEY=value\n", 2)]
    [InlineData("A='quoted' trailing\n", 1)]
    [InlineData("A=1\nB=\"unclosed\nC=3\n", 2)]
    [InlineData("A=1\r\nB='multi\r\nline'\r\n=missing name\r\n", 4)]
    [InlineData("\u00a0TOKEN=value\n", 1)]
    [InlineData("A=1\n\vBROKEN\nB=2\n", 2)]
    [InlineData("A=1\r\n \t\fBROKEN\r\nB=2\r\n", 2)]
    [InlineData("A=1\n\rBROKEN\nB=2\n", 2)]
    public void Read_dot_env_with_an_invalid_line_throws_with_its_line_number(string text, int line)
    {
        _file.WriteText(text);

        var result = Assert.Throws<FormatException>(_file.ReadDotEnv);

        Assert.StartsWith($"Line {line} ", result.Message);
    }

    [Fact]
    public void Write_dot_env_values_read_back_unchanged()
    {
        KeyValuePair<string, string>[] variables =
        [
            new("EMPTY", ""),
            new("PADDED", "  padded  "),
            new("HASH", "a # b"),
            new("SINGLE_QUOTE", "it's"),
            new("DOUBLE_QUOTE", "say \"hi\""),
            new("BOTH_QUOTES", "it's \"quoted\""),
            new("WINDOWS_PATH", @"C:\tools\"),
            new("ESCAPED_QUOTE", "it's \\\" done\\"),
            new("LINES", "first\nsecond\r\nthird\r"),
            new("TAB", "a\tb"),
            new("REFERENCE", "$HOME and ${PATH}"),
            new("UNICODE", "café ☃"),
            new("export", "name"),
        ];

        _file.WriteDotEnv(variables);
        var result = _file.ReadDotEnv();

        Assert.Equal(variables, result.ToArray());
    }

    [Fact]
    public void Write_dot_env_quotes_only_values_that_need_it()
    {
        _file.WriteDotEnv(new OrderedDictionary<string, string>
        {
            ["NODE_ENV"] = "production",
            ["API_URL"] = "https://example.com/api",
            ["EMPTY"] = "",
            ["GREETING"] = "hello world",
            ["REFERENCE"] = "token-${HOME}",
            ["MESSAGE"] = "it's\ndone",
        });

        var result = _file.ReadLines();

        Assert.Equal(
            [
                "NODE_ENV=production",
                "API_URL=https://example.com/api",
                "EMPTY=",
                "GREETING='hello world'",
                "REFERENCE='token-${HOME}'",
                "MESSAGE=\"it's\\ndone\"",
            ],
            result);
    }

    [Fact]
    public void Write_dot_env_replaces_the_existing_contents()
    {
        _file.WriteText("# Old settings\nOLD=value\n");

        _file.WriteDotEnv([new("A", "1")]);

        Assert.Equal($"A=1{Environment.NewLine}", _file.ReadText());
    }

    [Theory]
    [InlineData("MY KEY")]
    [InlineData("A=B")]
    [InlineData("A\n")]
    [InlineData("")]
    public void Write_dot_env_with_an_invalid_name_throws_without_changing_the_file(string name)
    {
        _file.WriteText("ORIGINAL=value\n");

        var result = Assert.Throws<ArgumentException>(() => _file.WriteDotEnv([new("VALID", "value"), new(name, "value")]));

        Assert.Equal("variables", result.ParamName);
        Assert.Equal("ORIGINAL=value\n", _file.ReadText());
    }

    [Fact]
    public void Update_dot_env_changes_values_in_place_and_keeps_the_rest_of_the_file()
    {
        _file.WriteText(
            "# Database\r\n"
            + "export DB_HOST=localhost # local only\r\n"
            + "DB_PASSWORD='old secret'\r\n"
            + "\r\n"
            + "  GREETING = \"hello\"\r\n"
            + "API_KEY= # required\r\n"
            + "UNCHANGED=\"kept\\tas written\"\r\n");
        var variables = _file.ReadDotEnv();
        variables["DB_HOST"] = "db.internal";
        variables["DB_PASSWORD"] = "new secret";
        variables["GREETING"] = "hello world";
        variables["API_KEY"] = "abc123";

        _file.UpdateDotEnv(variables);

        Assert.Equal(
            "# Database\r\n"
            + "export DB_HOST=db.internal # local only\r\n"
            + "DB_PASSWORD='new secret'\r\n"
            + "\r\n"
            + "  GREETING = \"hello world\"\r\n"
            + "API_KEY=abc123 # required\r\n"
            + "UNCHANGED=\"kept\\tas written\"\r\n",
            _file.ReadText());
    }

    [Fact]
    public void Update_dot_env_keeps_variables_that_are_not_supplied()
    {
        _file.WriteText("# Ports\nAPI_PORT=5000\nWEB_PORT=3000\n");

        _file.UpdateDotEnv([new("API_PORT", "5001")]);

        Assert.Equal("# Ports\nAPI_PORT=5001\nWEB_PORT=3000\n", _file.ReadText());
    }

    [Fact]
    public void Update_dot_env_appends_new_variables()
    {
        _file.WriteText("A=1\n# Keep this comment\nLAST=end");

        _file.UpdateDotEnv([new("D", "new"), new("E", "two words")]);

        Assert.Equal("A=1\n# Keep this comment\nLAST=end\nD=new\nE='two words'\n", _file.ReadText());
    }

    [Fact]
    public void Update_dot_env_replaces_the_whole_existing_value()
    {
        _file.WriteText(
            "MULTILINE='first\nsecond'\n"
            + "HASHED=#old\n"
            + "ESCAPED=\"old\\\" secret #suffix\" # comment\n");

        _file.UpdateDotEnv([new("MULTILINE", "new"), new("HASHED", "new"), new("ESCAPED", "new")]);

        Assert.Equal("MULTILINE='new'\nHASHED=new\nESCAPED=\"new\" # comment\n", _file.ReadText());
    }

    [Fact]
    public void Update_dot_env_with_a_repeated_name_changes_the_last_assignment()
    {
        _file.WriteText("MODE=first\nMODE=second\n");

        _file.UpdateDotEnv([new("MODE", "third")]);

        Assert.Equal("MODE=first\nMODE=third\n", _file.ReadText());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Update_dot_env_keeps_whether_the_file_has_a_byte_order_mark(bool byteOrderMark)
    {
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: byteOrderMark);
        _file.WriteText("A=1\n", encoding);

        _file.UpdateDotEnv([new("A", "2")]);

        Assert.Equal([.. encoding.GetPreamble(), .. encoding.GetBytes("A=2\n")], File.ReadAllBytes(_file));
    }

    [Fact]
    public void Update_dot_env_with_an_invalid_new_name_throws_without_changing_the_file()
    {
        _file.WriteText("ORIGINAL=value\n");

        var result = Assert.Throws<ArgumentException>(() => _file.UpdateDotEnv([new("MY KEY", "value")]));

        Assert.Equal("variables", result.ParamName);
        Assert.Equal("ORIGINAL=value\n", _file.ReadText());
    }

    [Fact]
    public void Update_dot_env_with_a_missing_file_throws_without_creating_it()
    {
        _file.Delete();

        Assert.Throws<FileNotFoundException>(() => _file.UpdateDotEnv([new("A", "1")]));
        Assert.False(_file.Exists);
    }
}
