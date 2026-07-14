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
                ListApps();
                return 0;
            }

            return args[0] switch
                {
                    ":new" => CreateApp(args),
                    ":init" => InitCommand.Run(args),
                    ":help" => ShowHelp(args),
                    var command when command.StartsWith(':') => Fail($"Unknown command '{command}'."),
                    var appName => await RunAppAsync(appName, args[1..])
                };
        }
        catch (DotNetDoConfigurationException exception)
        {
            return Fail(exception.Message);
        }
    }

    static int CreateApp(string[] args)
    {
        if (args.Length != 2)
            return Fail("Usage: do :new <name>");

        var appName = args[1];
        if (!AppScaffolding.IsValidName(appName))
            return Fail("App name must be a file stem using letters, numbers, '_', '-', or '.'. Do not include '.cs'.");

        var relativeFile = Do.ScriptsPath / $"{appName}.cs";
        var file = Do.RootDirectory / relativeFile;
        if (file.IsExistingFile)
            return Fail($"{relativeFile} already exists.");

        Do.ScriptsDirectory.EnsureDirectoryExists();
        File.WriteAllText(file, AppScaffolding.Template(appName));
        AppScaffolding.MakeExecutableIfUnix(file);
        Console.WriteLine($"Created {relativeFile}");
        return 0;
    }

    static async Task<int> RunAppAsync(string appName, string[] appArgs)
    {
        if (!AppScaffolding.IsValidName(appName))
            return Fail("App name must be a file stem using letters, numbers, '_', '-', or '.'. Do not include '.cs'.");

        var relativeFile = Do.ScriptsPath / $"{appName}.cs";
        var file = Do.RootDirectory / relativeFile;
        if (!file.IsExistingFile)
            return Fail($"{relativeFile} does not exist.");

        var startInfo = new ProcessStartInfo("dotnet") { UseShellExecute = false };

        startInfo.ArgumentList.Add(file);
        if (appArgs.Length > 0)
        {
            startInfo.ArgumentList.Add("--");
            foreach (var arg in appArgs)
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
            return AppScaffolding.IsValidName(args[1])
                ? AppHelp.Show(args[1])
                : Fail("App name must be a file stem using letters, numbers, '_', '-', or '.'. Do not include '.cs'.");

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

    static void ListApps()
    {
        if (!Do.ScriptsDirectory.IsExistingDirectory)
            return;

        var apps = Directory
            .EnumerateFiles(Do.ScriptsDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && AppScaffolding.IsValidName(name))
            .Order(StringComparer.OrdinalIgnoreCase);

        foreach (var app in apps)
            Console.WriteLine(app);
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

}
