using System.Text.RegularExpressions;

namespace DotNetDo;

static partial class TaskName
{
    public const string InvalidMessage = "Task name must use letters, numbers, '_', '-', or '.'. Do not include '.cs'.";

    public static bool IsValid(string name) =>
        !name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && NameRegex().IsMatch(name);

    [GeneratedRegex("^[A-Za-z0-9_.-]+$")]
    private static partial Regex NameRegex();
}
