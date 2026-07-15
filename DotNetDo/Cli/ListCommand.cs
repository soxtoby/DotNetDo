namespace DotNetDo.Cli;

static class ListCommand
{
    public static int Run()
    {
        if (!Do.ScriptsDirectory.IsExistingDirectory)
            return 0;

        var tasks = Directory
            .EnumerateFiles(Do.ScriptsDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && TaskScaffolding.IsValidName(name))
            .Order(StringComparer.OrdinalIgnoreCase);

        foreach (var task in tasks)
            Console.WriteLine(task);
        return 0;
    }
}
