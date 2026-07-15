namespace DotNetDo.Cli;

static class NewCommand
{
    public static int Run(string[] args)
    {
        if (args.Length != 2)
            return Fail("Usage: do :new <name>");

        var taskName = args[1];
        if (!TaskScaffolding.IsValidName(taskName))
            return Fail(TaskScaffolding.InvalidNameMessage);

        var relativeFile = Do.ScriptsPath / $"{taskName}.cs";
        var file = Do.RootDirectory / relativeFile;
        if (file.IsExistingFile)
            return Fail($"{relativeFile} already exists.");

        Do.ScriptsDirectory.EnsureDirectoryExists();
        TaskScaffolding.Create(file, taskName);
        Console.WriteLine($"Created {relativeFile}");
        return 0;
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
