using System.Diagnostics.CodeAnalysis;
using Tomlyn.Model;

namespace DotNetDo.Cli;

static class InitCommand
{
    const string DefaultSolutionFolder = "scripts";
    const string WindowsLauncher = "do.cmd";
    const string WindowsLauncherContent = "@call dnx DotNetDo -- %*\r\n@exit /b %ERRORLEVEL%\r\n";
    const string UnixLauncher = "do";
    const string UnixLauncherContent = "#!/usr/bin/env sh\nexec dnx DotNetDo -- \"$@\"\n";

    public static Task<int> Run(string[] args) => Run(args, Do.WorkingDirectory, new DotNetClient());

    internal static async Task<int> Run(string[] args, AbsolutePath root, IPackageVersionResolver packageVersions)
    {
        if (args.Length != 1)
        {
            await Console.Error.WriteLineAsync("Usage: dotnetdo :init");
            return 1;
        }

        if (TryCollectInitialization(root, out var initialization))
        {
            try
            {
                await ApplyInitialization(root, initialization, packageVersions);
                return 0;
            }
            catch (PackageLookupException exception)
            {
                await Console.Error.WriteLineAsync(exception.Message);
                return 1;
            }
        }
        else
        {
            await Console.Error.WriteLineAsync("Initialization cancelled.");
            return 1;
        }
    }

    static bool TryCollectInitialization(AbsolutePath root, [NotNullWhen(true)] out Initialization? initialization)
    {
        initialization = null;

        try
        {
            if (ShouldAvoidCreatingNestedWorkspace(root))
                return false;

            initialization = (root / WorkspaceConfiguration.FileName).IsExistingFile
                ? InitializeFromExistingConfig(root)
                : new Initialization();

            if (initialization.ScriptsPath is null)
                initialization = initialization with { ScriptsPath = PromptScriptsPath(root) };

            if ((root / initialization.ScriptsPath).GlobFiles("*.cs").Length == 0)
                initialization = initialization with { TaskName = PromptTaskName() };

            if (initialization.SolutionPath is null)
                initialization = initialization with { SolutionPath = SelectSolution(root) };

            if (initialization.SolutionPath is not null && PromptAddScriptsToSolution(initialization.SolutionPath!))
                initialization = initialization with { AddScriptsToSolution = true };

            if (PromptOverwriteLaunchers(root))
                initialization = initialization with { OverwriteLaunchers = true };

            return true;
        }
        catch (InitializationCancelledException)
        {
            return false;
        }
    }

    static bool ShouldAvoidCreatingNestedWorkspace(AbsolutePath root)
    {
        return WorkspaceConfiguration.FindClosest(root) is { } existingConfig
            && existingConfig.Parent != root
            && !PromptCreateNestedWorkspace(root, existingConfig);
    }

    static async Task ApplyInitialization(AbsolutePath root, Initialization initialization, IPackageVersionResolver packageVersions)
    {
        if (UpdateConfigFile(root, initialization))
            Console.WriteLine($"Updated {root / WorkspaceConfiguration.FileName}");

        var scriptsPath = root / initialization.ScriptsPath!;
        if (!scriptsPath.Exists)
        {
            scriptsPath.EnsureDirectoryExists();
            Console.WriteLine($"Created {initialization.ScriptsPath}");
        }

        if (initialization.TaskName is not null)
        {
            var taskFile = scriptsPath / $"{initialization.TaskName}.cs";
            var packageVersion = await packageVersions.FindLatest(TaskScaffolding.Package, prerelease: false, root);
            TaskScaffolding.Create(taskFile, initialization.TaskName, packageVersion);
            Console.WriteLine($"Created {initialization.ScriptsPath! / $"{initialization.TaskName}.cs"}");
        }

        if (initialization.SolutionPath is not null && initialization.AddScriptsToSolution)
        {
            await SolutionFolderSync.Run(root / initialization.SolutionPath!, scriptsPath, initialization.SolutionFolder);
            Console.WriteLine($"Added scripts to {initialization.SolutionFolder} solution folder");
        }

        WriteLauncher(root, WindowsLauncher, WindowsLauncherContent, initialization.OverwriteLaunchers);

        if (WriteLauncher(root, UnixLauncher, UnixLauncherContent, initialization.OverwriteLaunchers))
        {
            FileScaffolding.MakeExecutableIfUnix(root / UnixLauncher);
            if (OperatingSystem.IsWindows())
                Console.WriteLine($"Before committing, run: git add --chmod=+x {UnixLauncher}");
        }

        if (initialization.TaskName is not null)
            Console.WriteLine($"Run with: ./do {initialization.TaskName}");
    }

    static bool UpdateConfigFile(AbsolutePath root, Initialization initialization)
    {
        var configFile = root / WorkspaceConfiguration.FileName;

        var config = configFile.IsExistingFile ? configFile.ReadToml<TomlTable>() : null;
        config ??= new TomlTable();

        var initialKeys = config.Keys.ToArray();

        if (!config.ContainsKey("scripts-path"))
            config["scripts-path"] = initialization.ScriptsPath!.UnixPath;

        if (!config.ContainsKey("solution-path") && initialization.SolutionPath is { } solutionPath)
            config["solution-path"] = solutionPath.UnixPath;

        if (initialization.AddScriptsToSolution)
            config["solution-folder"] = initialization.SolutionFolder;

        var write = !config.Keys.SequenceEqual(initialKeys);
        if (write)
            configFile.WriteToml(config);

        return write;
    }

    static Initialization InitializeFromExistingConfig(AbsolutePath root)
    {
        var config = WorkspaceConfiguration.Load(root);
        return new Initialization
            {
                ScriptsPath = config.ScriptsPath,
                SolutionFolder = config.SolutionFolder ?? DefaultSolutionFolder,
                SolutionPath = config.SolutionPath
            };
    }

    /// <returns><see langword="true"/> if the launcher was created rather than overwritten or left unchanged.</returns>
    static bool WriteLauncher(AbsolutePath root, string name, string content, bool overwrite)
    {
        var launcher = root / name;
        var create = !launcher.Exists;
        if (!create && !(overwrite && LauncherDiffers(root, name, content)))
            return false;

        launcher.WriteText(content);
        Console.WriteLine($"{(create ? "Created" : "Updated")} {name} launcher");
        return create;
    }

    static bool PromptOverwriteLaunchers(AbsolutePath root)
    {
        var differing = new[] { (WindowsLauncher, WindowsLauncherContent), (UnixLauncher, UnixLauncherContent) }
            .Where(launcher => LauncherDiffers(root, launcher.Item1, launcher.Item2))
            .Select(launcher => launcher.Item1)
            .ToArray();
        return differing.Length > 0
            && PromptYesNo($"Overwrite launchers that differ from the default ({string.Join(", ", differing)})?", defaultValue: false);
    }

    static bool LauncherDiffers(AbsolutePath root, string name, string content)
    {
        var launcher = root / name;
        // Compare without line endings, which Git may rewrite on checkout.
        return launcher.IsExistingFile
            && launcher.ReadText().ReplaceLineEndings("\n") != content.ReplaceLineEndings("\n");
    }

    static bool PromptCreateNestedWorkspace(AbsolutePath root, AbsolutePath ancestorConfiguration)
    {
        Console.WriteLine("The current directory is inside an existing DotNetDo workspace.");
        Console.WriteLine($"Existing workspace root: {ancestorConfiguration.Parent}");
        return PromptYesNo($"Create a nested DotNetDo workspace in '{root}'?", defaultValue: false);
    }

    static RelativePath PromptScriptsPath(AbsolutePath root)
    {
        while (true)
        {
            Console.Write("Scripts path (default: scripts): ");
            var input = Console.ReadLine();
            if (input is null)
                throw new InitializationCancelledException();

            try
            {
                var path = WorkspaceConfiguration.ParseRootRelativePath(input.Length == 0 ? DefaultSolutionFolder : input);
                if ((root / path).IsExistingFile)
                    Console.Error.WriteLine("Specified path conflicts with an existing file.");
                else
                    return path;
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
            if (TaskName.IsValid(input))
                return input;
            Console.Error.WriteLine(TaskName.InvalidMessage);
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

    static bool PromptAddScriptsToSolution(RelativePath solutionPath)
        => PromptYesNo($"Add scripts to '{solutionPath.UnixPath}'?", defaultValue: true);

    static bool PromptYesNo(string prompt, bool defaultValue)
    {
        while (true)
        {
            Console.Write($"{prompt} {(defaultValue ? "[Y/n]" : "[y/N]")}: ");
            var input = Console.ReadLine() ?? throw new InitializationCancelledException();
            if (input.Length == 0)
                return defaultValue;
            if (input.Equals("y", StringComparison.OrdinalIgnoreCase) || input.Equals("yes", StringComparison.OrdinalIgnoreCase))
                return true;
            if (input.Equals("n", StringComparison.OrdinalIgnoreCase) || input.Equals("no", StringComparison.OrdinalIgnoreCase))
                return false;
            Console.Error.WriteLine("Enter y or n.");
        }
    }

    sealed record Initialization
    {
        public RelativePath? ScriptsPath { get; init; }
        public string? TaskName { get; init; }
        public RelativePath? SolutionPath { get; init; }
        public bool AddScriptsToSolution { get; init; }
        public string SolutionFolder { get; init; } = DefaultSolutionFolder;
        public bool OverwriteLaunchers { get; init; }
    }

    sealed class InitializationCancelledException : Exception;
}
