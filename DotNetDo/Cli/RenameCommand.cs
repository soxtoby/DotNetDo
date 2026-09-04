namespace DotNetDo.Cli;

static class RenameCommand
{
    public static async Task<int> Run(string[] args)
    {
        if (args.Length != 3)
            return Fail("Usage: ./do :rename <old-name> <new-name>");

        var oldName = args[1];
        var newName = args[2];
        if (!TaskName.IsValid(oldName) || !TaskName.IsValid(newName))
            return Fail(TaskName.InvalidMessage);

        var root = Do.RootDirectory;
        var configuration = WorkspaceConfiguration.Load(root);
        var oldRelativeFile = configuration.ScriptsPath / $"{oldName}.cs";
        var newRelativeFile = configuration.ScriptsPath / $"{newName}.cs";
        var oldFile = root / oldRelativeFile;
        var newFile = root / newRelativeFile;
        var changesOnlyCase = !oldName.Equals(newName, StringComparison.Ordinal)
            && oldName.Equals(newName, StringComparison.OrdinalIgnoreCase);

        if (!oldFile.IsExistingFile)
            return Fail($"{oldRelativeFile} does not exist.");
        if (newFile.Exists && !changesOnlyCase)
            return Fail($"{newRelativeFile} already exists.");
        if (configuration.MetaTasks.ContainsKey(newName))
            return Fail($"Task '{newName}' is already defined in '{WorkspaceConfiguration.FileName}'.");

        try
        {
            Move(oldFile, newFile, changesOnlyCase);
        }
        catch (IOException) when (newFile.Exists)
        {
            return Fail($"{newRelativeFile} already exists.");
        }

        try
        {
            if (configuration is { SolutionPath: { } solutionPath, SolutionFolder: { } solutionFolder })
                await SolutionFolderSync.Run(root / solutionPath, root / configuration.ScriptsPath, solutionFolder);
        }
        catch
        {
            if (newFile.IsExistingFile && !oldFile.Exists)
                Move(newFile, oldFile, changesOnlyCase);
            throw;
        }

        Console.WriteLine($"Renamed {oldRelativeFile} to {newRelativeFile}");
        return 0;
    }

    static void Move(AbsolutePath source, AbsolutePath destination, bool changesOnlyCase)
    {
        if (!changesOnlyCase)
        {
            source.MoveTo(destination);
            return;
        }

        var temporary = source.Parent / $".dotnetdo-rename-{Guid.NewGuid():N}.tmp";
        source.MoveTo(temporary);
        try
        {
            temporary.MoveTo(destination);
        }
        catch
        {
            if (temporary.IsExistingFile)
                temporary.MoveTo(source);
            throw;
        }
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
