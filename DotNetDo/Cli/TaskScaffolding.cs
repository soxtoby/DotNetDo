using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace DotNetDo.Cli;

static partial class TaskScaffolding
{
    public const string InvalidNameMessage = "Task name must be a file stem using letters, numbers, '_', '-', or '.'. Do not include '.cs'.";

    public static bool IsValidName(string name) =>
        !name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && NameRegex().IsMatch(name);

    public static string Template(string name) =>
        $"""
        #!/usr/bin/env dotnet
        #:package DotNetDo@*
        using DotNetDo;

        Console.WriteLine("Hello from {name}");
        """;

    public static void MakeExecutableIfUnix(string fileName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;

        try
        {
            File.SetUnixFileMode(fileName,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
        catch
        {
            // Best effort for filesystems without Unix mode support.
        }
    }

    [GeneratedRegex("^[A-Za-z0-9_.-]+$")]
    private static partial Regex NameRegex();
}
