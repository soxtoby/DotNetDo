using NuGet.Versioning;

namespace DotNetDo.Cli;

static class NewCommand
{
    public static Task<int> Run(string[] args) => Run(args, Do.RootDirectory, new DotNetClient());

    internal static async Task<int> Run(
        string[] args,
        AbsolutePath root,
        IPackageVersionResolver packageVersions)
    {
        if (args.Length != 2)
            return Fail("Usage: ./do :new <name>");

        var taskName = args[1];
        if (!TaskName.IsValid(taskName))
            return Fail(TaskName.InvalidMessage);

        var configuration = WorkspaceConfiguration.Load(root);
        var relativeFile = configuration.ScriptsPath / $"{taskName}.cs";
        var scriptsDirectory = root / configuration.ScriptsPath;
        var file = root / relativeFile;
        if (file.IsExistingFile)
            return Fail($"{relativeFile} already exists.");

        NuGetVersion packageVersion;
        try
        {
            packageVersion = await packageVersions.FindLatest(TaskScaffolding.Package, prerelease: false, root);
        }
        catch (PackageLookupException exception)
        {
            return Fail(exception.Message);
        }

        scriptsDirectory.EnsureDirectoryExists();
        TaskScaffolding.Create(file, taskName, packageVersion);
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
