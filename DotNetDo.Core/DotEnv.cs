using System.Text;
using System.Text.RegularExpressions;

namespace DotNetDo;

/// <summary>Dotenv parsing, formatting, and in-place updates. Values are always literal; variable references are never expanded.</summary>
static partial class DotEnv
{
    public static OrderedDictionary<string, string> Parse(string text) => Variables(Assignments(text));

    public static string Format(IEnumerable<KeyValuePair<string, string>> variables) =>
        Lines(variables, Environment.NewLine, nameof(variables));

    public static string Update(string text, IEnumerable<KeyValuePair<string, string>> variables)
    {
        var assignments = Assignments(text);
        var existingNames = assignments.Select(Name).ToHashSet(StringComparer.Ordinal);
        var changes = new OrderedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var variable in variables)
            changes[variable.Key] = variable.Value;

        // Only the last assignment of a repeated name is effective, so it's the only one worth changing.
        var replacements = assignments
            .GroupBy(Name)
            .Select(group => group.Last())
            .Where(assignment => changes.TryGetValue(Name(assignment), out var value) && value != Value(assignment))
            .Select(assignment => Replacement(assignment, changes[Name(assignment)]));
        var additions = Lines(changes.Where(variable => !existingNames.Contains(variable.Key)), text.DetectedNewLine(), nameof(variables));

        var result = new StringBuilder(text);
        foreach (var edit in replacements.OrderByDescending(edit => edit.Index))
            result.Remove(edit.Index, edit.Length).Insert(edit.Index, edit.Replacement);
        if (additions.Length > 0 && result.Length > 0 && result[^1] != '\n')
            result.Append(text.DetectedNewLine());
        return result.Append(additions).ToString();
    }

    static List<Match> Assignments(string text)
    {
        var lines = LineRegex().Matches(text);

        if (lines.FirstOrDefault(line => line.Groups["invalid"].Success) is { } invalid)
            throw new FormatException($"Line {text.AsSpan(0, invalid.Index).Count('\n') + 1} is not a valid .env variable assignment, comment, or blank line.");

        return [.. lines.Where(line => line.Groups["name"].Success)];
    }

    static OrderedDictionary<string, string> Variables(IEnumerable<Match> assignments)
    {
        var variables = new OrderedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var assignment in assignments)
            variables[Name(assignment)] = Value(assignment);
        return variables;
    }

    static string Name(Match assignment) => assignment.Groups["name"].Value;

    // Normalizing keeps multiline values independent of the checkout's line endings.
    static string Value(Match assignment) =>
        assignment.Groups["double"].Success ? Unescape(assignment.Groups["double"].Value.Replace("\r\n", "\n"))
        : assignment.Groups["single"].Success ? assignment.Groups["single"].Value.Replace("\r\n", "\n")
        : assignment.Groups["unquoted"].Value;

    static string Unescape(string value) =>
        EscapeRegex().Replace(value, escape => escape.Groups["character"].Value switch
        {
            "n" => "\n",
            "r" => "\r",
            "t" => "\t",
            "\\" or "\"" => escape.Groups["character"].Value,
            _ => escape.Value,
        });

    static Edit Replacement(Match assignment, string value)
    {
        var current = assignment.Groups["value"];
        return new(current.Index, current.Length, Requote(assignment, value));
    }

    // Keeps the assignment's existing quote style when it can represent the new value.
    static string Requote(Match assignment, string value) =>
        assignment.Groups["double"].Success ? DoubleQuote(value)
        : assignment.Groups["single"].Success && CanSingleQuote(value) ? $"'{value}'"
        : Quote(value);

    static string Lines(IEnumerable<KeyValuePair<string, string>> variables, string newLine, string parameterName) =>
        string.Concat(variables.Select(variable => $"{ValidName(variable.Key, parameterName)}={Quote(variable.Value)}{newLine}"));

    static string ValidName(string name, string parameterName) =>
        NameRegex().IsMatch(name)
            ? name
            : throw new ArgumentException($"'{name}' is not a valid .env variable name. Use letters, numbers, '_', '-', or '.'.", parameterName);

    // Unquoted and single-quoted values read literally in other dotenv readers and POSIX shells.
    // Double quotes are the fallback for values that need escapes.
    static string Quote(string value) =>
        UnquotedValueRegex().IsMatch(value) ? value
        : CanSingleQuote(value) ? $"'{value}'"
        : DoubleQuote(value);

    static bool CanSingleQuote(string value) => !value.AsSpan().ContainsAny('\'', '\n', '\r');

    static string DoubleQuote(string value) =>
        $"\"{value.Replace("\\", @"\\").Replace("\"", "\\\"").Replace("\n", @"\n").Replace("\r", @"\r")}\"";

    readonly record struct Edit(int Index, int Length, string Replacement);

    // Lines may end with LF or CRLF, so updates can keep the file's own line endings.
    // A # starts a comment only after whitespace or a closing quote, so unquoted values may contain or start with one.
    // An empty value is positioned directly after '=', so filling it in keeps the whitespace before an inline comment.
    // Any other content, including other whitespace characters, is invalid rather than skipped.
    // Atomic whitespace runs keep matching linear when a line contains long runs of spaces.
    [GeneratedRegex("""
        ^[ \t]*
        (?:
            (?:export[ \t]+)?(?<name>[A-Za-z0-9_.-]+)[ \t]*=
            (?:
                (?>[ \t]*)
                (?<value>
                    '(?<single>[^']*)'
                  | "(?<double>(?:\\[\s\S]|[^"\\])*)"
                  | (?<unquoted>(?:(?<==)\#|[^'"\s\#])(?>(?:[^ \t\r\n]+|(?>[ \t]+)(?!\#|\r?\n|\r?\z))*))
                )
              | (?<value>)
            )
          | (?<invalid>(?!\#|\r?\n|\r?\z)[^\n]*)
        )?
        [ \t]*(?:(?<![^ \t\n'"])\#[^\r\n]*)?(?=\r?\n|\r?\z)
        """, RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex LineRegex();

    [GeneratedRegex(@"\\(?<character>[\s\S])")]
    private static partial Regex EscapeRegex();

    [GeneratedRegex(@"\A[A-Za-z0-9_.-]+\z")]
    private static partial Regex NameRegex();

    [GeneratedRegex(@"\A[A-Za-z0-9_.,:/@%+=-]*\z")]
    private static partial Regex UnquotedValueRegex();
}
