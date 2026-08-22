namespace DotNetDo.Cli;

static class NewCommand
{
    public static async Task<int> Run(string[] args)
    {
        if (args.Length != 2)
            return Fail("Usage: dotnet do :new <name>");

        var taskName = args[1];
        if (!TaskName.IsValid(taskName))
            return Fail(TaskName.InvalidMessage);

        var root = Do.RootDirectory;
        var configuration = WorkspaceConfiguration.Load(root);
        var relativeFile = configuration.ScriptsPath / $"{taskName}.cs";
        var scriptsDirectory = root / configuration.ScriptsPath;
        var file = root / relativeFile;
        if (file.IsExistingFile)
            return Fail($"{relativeFile} already exists.");

        scriptsDirectory.EnsureDirectoryExists();
        TaskScaffolding.Create(file, taskName);
        try
        {
            if (configuration is { SolutionPath: { } solutionPath, SolutionFolder: { } solutionFolder })
                await SolutionFolderSync.Run(root / solutionPath, scriptsDirectory, solutionFolder);
        }
        catch
        {
            if (file.IsExistingFile)
                file.Delete();
            throw;
        }

        Console.WriteLine($"Created {relativeFile}");
        return 0;
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
