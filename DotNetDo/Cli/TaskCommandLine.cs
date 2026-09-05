using System.Text.RegularExpressions;

namespace DotNetDo.Cli;

readonly partial record struct TaskCommandLine(IReadOnlyList<string> Parameters, IReadOnlyList<string> Arguments)
{
    public static TaskCommandLine ParseConfigured(string value) =>
        FromArguments([.. ArgumentPattern().Matches(value).Select(match => Unquote(match.Value))]);

    public static TaskCommandLine FromArguments(string[] arguments)
    {
        var separator = Array.IndexOf(arguments, "--");
        return separator < 0
            ? new(arguments, [])
            : new(arguments[..separator], arguments[(separator + 1)..]);
    }

    public TaskCommandLine AppendFixed(TaskCommandLine fixedCommandLine) =>
        new([.. Parameters, .. fixedCommandLine.Parameters], [.. Arguments, .. fixedCommandLine.Arguments]);

    public TaskCommandLine AppendParameters(params string[] parameters) =>
        this with { Parameters = [.. Parameters, .. parameters] };

    public string[] ToArguments() =>
        Arguments.Count == 0 ? [.. Parameters] : [.. Parameters, "--", .. Arguments];

    public string Render() =>
        string.Join(" ", ToArguments().Select(argument => argument.QuotedArgument()));

    static string Unquote(string argument)
    {
        var quoted = false;
        return QuotePattern().Replace(
            argument,
            match =>
                {
                    var backslashes = match.Groups[1].Length;
                    var quotes = match.Length - backslashes;
                    var escaped = backslashes % 2 != 0;
                    var literalQuote = escaped || quoted && quotes == 2;

                    // An escaped quote is literal; one remaining quote opens or closes a quoted section.
                    if (quotes - (escaped ? 1 : 0) == 1)
                        quoted = !quoted;
                    return new string('\\', backslashes / 2) + (literalQuote ? "\"" : "");
                });
    }

    [GeneratedRegex("""
        (?:
            [^\s"\\] | \\[\\"] | \\(?![\\"])  # Unquoted text and backslashes
          | " (?: [^"\\] | \\[\\"] | \\(?![\\"]) | "" )* (?: " | $ )  # Quoted text
        )+
        """,
        RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex ArgumentPattern();

    [GeneratedRegex("""(\\*)"{1,2}""")]
    private static partial Regex QuotePattern();
}