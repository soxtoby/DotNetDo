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
        #:package DotNetDo.Core@*
        using DotNetDo;

        Console.WriteLine("Hello from {name}");
        """;

    public static void Create(AbsolutePath file, string name)
    {
        var created = false;
        try
        {
            using (var stream = new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                created = true;
                writer.Write(Template(name));
            }
            FileScaffolding.MakeExecutableIfUnix(file);
        }
        catch
        {
            if (created)
                file.Delete();
            throw;
        }
    }

    [GeneratedRegex("^[A-Za-z0-9_.-]+$")]
    private static partial Regex NameRegex();
}
