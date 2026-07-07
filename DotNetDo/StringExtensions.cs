using System.Text;

namespace DotNetDo;

public static class StringExtensions
{
    public static string QuotedArgument(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return NeedsQuotes(value)
            ? Quote(value)
            : value;
    }

    static bool NeedsQuotes(string value) =>
        value.Length == 0 || value.Any(char.IsWhiteSpace) || value.Contains('"');

    static string Quote(string value)
    {
        var quoted = new StringBuilder();
        quoted.Append('"');

        var backslashes = 0;
        foreach (var character in value)
        {
            switch (character)
            {
                case '\\':
                    backslashes++;
                    continue;
                case '"':
                    quoted.Append('\\', backslashes * 2 + 1);
                    quoted.Append('"');
                    break;
                default:
                    quoted.Append('\\', backslashes);
                    quoted.Append(character);
                    break;
            }

            backslashes = 0;
        }

        quoted.Append('\\', backslashes * 2);
        quoted.Append('"');
        return quoted.ToString();
    }
}