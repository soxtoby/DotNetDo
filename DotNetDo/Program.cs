using System.Diagnostics;
using DotNetDo;

return await Cli.RunAsync(args);

static class Cli
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                ListTasks();
                return 0;
            }

            return args[0] switch
                {
                    ":new" => CreateTask(args),
                    ":init" => InitCommand.Run(args),
                    ":help" => ShowHelp(args),
                    var command when command.StartsWith(':') => Fail($"Unknown command '{command}'."),
                    var taskName => await RunTaskAsync(taskName, args[1..])
                };
        }
        catch (DotNetDoConfigurationException exception)
        {
            return Fail(exception.Message);
        }
    }

    static int CreateTask(string[] args)
    {
        if (args.Length != 2)
            return Fail("Usage: do :new <name>");

        var taskName = args[1];
        if (!TaskScaffolding.IsValidName(taskName))
            return Fail("Task name must be a file stem using letters, numbers, '_', '-', or '.'. Do not include '.cs'.");

        var relativeFile = Do.ScriptsPath / $"{taskName}.cs";
        var file = Do.RootDirectory / relativeFile;
        if (file.IsExistingFile)
            return Fail($"{relativeFile} already exists.");

        Do.ScriptsDirectory.EnsureDirectoryExists();
        File.WriteAllText(file, TaskScaffolding.Template(taskName));
        TaskScaffolding.MakeExecutableIfUnix(file);
        Console.WriteLine($"Created {relativeFile}");
        return 0;
    }

    static async Task<int> RunTaskAsync(string taskName, string[] taskArgs)
    {
        if (!TaskScaffolding.IsValidName(taskName))
            return Fail("Task name must be a file stem using letters, numbers, '_', '-', or '.'. Do not include '.cs'.");

        var relativeFile = Do.ScriptsPath / $"{taskName}.cs";
        var file = Do.RootDirectory / relativeFile;
        if (!file.IsExistingFile)
            return Fail($"{relativeFile} does not exist.");

        var startInfo = new ProcessStartInfo("dotnet") { UseShellExecute = false };

        startInfo.ArgumentList.Add(file);
        if (taskArgs.Length > 0)
        {
            startInfo.ArgumentList.Add("--");
            foreach (var arg in taskArgs)
                startInfo.ArgumentList.Add(arg);
        }

        using var process = Process.Start(startInfo);
        if (process is null)
            return Fail("Failed to start dotnet.");

        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    static int ShowHelp(string[] args)
    {
        if (args.Length == 2)
            return TaskScaffolding.IsValidName(args[1])
                ? TaskHelp.Show(args[1])
                : Fail("Task name must be a file stem using letters, numbers, '_', '-', or '.'. Do not include '.cs'.");

        Console.WriteLine("""
            Usage:
              do
              do :init
              do :new <name>
              do :help <name>
              do :help
              do <name> [args...]
            """);
        return 0;
    }

    static void ListTasks()
    {
        if (!Do.ScriptsDirectory.IsExistingDirectory)
            return;

        var tasks = Directory
            .EnumerateFiles(Do.ScriptsDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && TaskScaffolding.IsValidName(name))
            .Order(StringComparer.OrdinalIgnoreCase);

        foreach (var task in tasks)
            Console.WriteLine(task);
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

}
