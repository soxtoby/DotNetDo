using Tomlyn;
using Tomlyn.Model;

namespace DotNetDo.Cli;

static class InitCommand
{
    public static int Run(string[] args)
    {
        if (args.Length != 1)
            return Fail("Usage: do :init");

        var root = AbsolutePath.Parse(Environment.CurrentDirectory);
        var configurationFile = root / "dotnetdo.toml";
        if (configurationFile.IsExistingFile)
            return Fail($"{configurationFile} already exists.");

        RelativePath scriptsPath;
        string taskName;
        RelativePath? solutionPath;
        try
        {
            var ancestorConfiguration = FindAncestorConfiguration(root);
            if (ancestorConfiguration is not null && !PromptCreateNestedWorkspace(root, ancestorConfiguration))
                return 0;

            scriptsPath = PromptScriptsPath();
            taskName = PromptTaskName();
            solutionPath = SelectSolution(root);
        }
        catch (InitializationCancelledException)
        {
            return Fail("Initialization cancelled.");
        }

        var scriptsDirectory = root / scriptsPath;
        var scriptFile = scriptsDirectory / $"{taskName}.cs";
        if (scriptFile.Exists)
            return Fail($"{scriptsPath.UnixPath}/{taskName}.cs already exists.");

        var createdDirectories = new List<AbsolutePath>();
        var scriptCreated = false;
        var configurationCreated = false;
        try
        {
            EnsureDirectories(root, scriptsDirectory, createdDirectories);
            TaskScaffolding.Create(scriptFile, taskName);
            scriptCreated = true;

            var configuration = new TomlTable { ["scripts-path"] = scriptsPath.UnixPath };
            if (solutionPath is not null)
                configuration["solution-path"] = solutionPath.UnixPath;
            using (var stream = new FileStream(configurationFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                configurationCreated = true;
                TomlSerializer.Serialize(stream, configuration);
            }
        }
        catch
        {
            if (configurationCreated)
                configurationFile.Delete();
            if (scriptCreated)
                scriptFile.Delete();
            foreach (var directory in createdDirectories)
                TryDeleteEmpty(directory);
            throw;
        }

        Console.WriteLine("Created dotnetdo.toml");
        Console.WriteLine($"{(createdDirectories.Count != 0 ? "Created" : "Reused")} scripts path: {scriptsPath.UnixPath}");
        Console.WriteLine($"Created {scriptsPath.UnixPath}/{taskName}.cs");
        if (solutionPath is not null)
            Console.WriteLine($"Selected solution: {solutionPath.UnixPath}");
        Console.WriteLine($"Run with: do {taskName}");
        return 0;
    }

    static void EnsureDirectories(AbsolutePath root, AbsolutePath directory, List<AbsolutePath> created)
    {
        if (directory == root || directory.IsExistingDirectory)
            return;
        EnsureDirectories(root, directory.Parent!, created);
        Directory.CreateDirectory(directory);
        created.Insert(0, directory);
    }

    static void TryDeleteEmpty(AbsolutePath directory)
    {
        try { Directory.Delete(directory); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    static AbsolutePath? FindAncestorConfiguration(AbsolutePath root)
    {
        for (var directory = root.Parent; directory is not null; directory = directory.Parent)
        {
            var configurationFile = directory / "dotnetdo.toml";
            if (configurationFile.IsExistingFile)
                return configurationFile;
        }
        return null;
    }

    static bool PromptCreateNestedWorkspace(AbsolutePath root, AbsolutePath ancestorConfiguration)
    {
        Console.WriteLine("The current directory is inside an existing DotNetDo workspace.");
        Console.WriteLine($"Existing workspace root: {ancestorConfiguration.Parent}");

        while (true)
        {
            Console.Write($"Create a nested DotNetDo workspace in '{root}'? [y/N]: ");
            var input = Console.ReadLine();
            if (input is null)
                throw new InitializationCancelledException();
            if (input.Length == 0 || input.Equals("n", StringComparison.OrdinalIgnoreCase) || input.Equals("no", StringComparison.OrdinalIgnoreCase))
                return false;
            if (input.Equals("y", StringComparison.OrdinalIgnoreCase) || input.Equals("yes", StringComparison.OrdinalIgnoreCase))
                return true;
            Console.Error.WriteLine("Enter y or n.");
        }
    }

    static RelativePath PromptScriptsPath()
    {
        while (true)
        {
            Console.Write("Scripts path (default: scripts): ");
            var input = Console.ReadLine();
            if (input is null)
                throw new InitializationCancelledException();
            
            try
            {
                return WorkspaceConfiguration.ParseRootRelativePath(input.Length == 0 ? "scripts" : input);
            }
            catch (ArgumentException)
            {
                Console.Error.WriteLine("Scripts path must be a non-empty root-relative path that remains within the workspace.");
            }
        }
    }

    static string PromptTaskName()
    {
        while (true)
        {
            Console.Write("Initial task name (default: build): ");
            var input = Console.ReadLine();
            if (input is null)
                throw new InitializationCancelledException();
            if (input.Length == 0)
                input = "build";
            if (TaskScaffolding.IsValidName(input))
                return input;
            Console.Error.WriteLine(TaskScaffolding.InvalidNameMessage);
        }
    }

    static RelativePath? SelectSolution(AbsolutePath root)
    {
        var solutions = root.GlobFiles(["**/*.sln", "**/*.slnx"])
            .Select(root.RelativePathTo)
            .OrderBy(path => path.Segments.Length)
            .ThenBy(path => path.UnixPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (solutions.Length <= 1)
            return solutions.SingleOrDefault();

        Console.WriteLine("Solutions:");
        for (var index = 0; index < solutions.Length; index++)
            Console.WriteLine($"  {index + 1}. {solutions[index].UnixPath}");
        while (true)
        {
            Console.Write("Select solution: ");
            var input = Console.ReadLine();
            if (input is null)
                throw new InitializationCancelledException();
            if (int.TryParse(input, out var selection) && selection >= 1 && selection <= solutions.Length)
                return solutions[selection - 1];
            Console.Error.WriteLine($"Select a number from 1 to {solutions.Length}.");
        }
    }

    sealed class InitializationCancelledException : Exception { }

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
