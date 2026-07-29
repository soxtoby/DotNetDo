using System.Text.RegularExpressions;

namespace DotNetDo.Cli;

static partial class TaskMetadata
{
    public static string? DiscoverDescription(string fileName)
    {
        var match = DescriptionRegex().Match(File.ReadAllText(fileName));
        if (!match.Success)
            return null;

        var description = Regex.Unescape(match.Groups["description"].Value)
            .Replace('\t', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
        return description.Length == 0 ? null : description;
    }

    [GeneratedRegex("""\[assembly:\s*(?:DotNetDo\.)?TaskDescription(?:Attribute)?\(\s*"(?<description>(?:\\.|[^"\\])*)"\s*\)\s*\]""")]
    private static partial Regex DescriptionRegex();
}
