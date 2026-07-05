using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

return await Cli.RunAsync(args);

static partial class Cli
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ListApps();
            return 0;
        }

        return args[0] switch
            {
                ":new" => CreateApp(args),
                ":help" => ShowHelp(),
                var command when command.StartsWith(':') => Fail($"Unknown command '{command}'."),
                var appName => await RunAppAsync(appName, args[1..])
            };
    }

    static int CreateApp(string[] args)
    {
        if (args.Length != 2)
            return Fail("Usage: do :new <name>");

        var appName = args[1];
        if (!IsValidAppName(appName))
            return Fail("App name must be a file stem using letters, numbers, '_', '-', or '.'. Do not include '.cs'.");

        var fileName = $"{appName}.cs";
        if (File.Exists(fileName))
            return Fail($"{fileName} already exists.");

        File.WriteAllText(fileName, NewAppTemplate(appName));
        MakeExecutableIfUnix(fileName);
        Console.WriteLine($"Created {fileName}");
        return 0;
    }

    static async Task<int> RunAppAsync(string appName, string[] appArgs)
    {
        if (!IsValidAppName(appName))
            return Fail("App name must be a file stem using letters, numbers, '_', '-', or '.'. Do not include '.cs'.");

        var fileName = $"{appName}.cs";
        if (!File.Exists(fileName))
            return Fail($"{fileName} does not exist.");

        var startInfo = new ProcessStartInfo("dotnet") { UseShellExecute = false };

        startInfo.ArgumentList.Add(fileName);
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

    static int ShowHelp()
    {
        Console.WriteLine("""
            Usage:
              do
              do :new <name>
              do :help
              do <name> [args...]
            """);
        return 0;
    }

    static void ListApps()
    {
        var apps = Directory
            .EnumerateFiles(Environment.CurrentDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name is not null && IsValidAppName(name))
            .Order(StringComparer.OrdinalIgnoreCase);

        foreach (var app in apps)
            Console.WriteLine(app);
    }

    static string NewAppTemplate(string appName) =>
        $"""
        #!/usr/bin/env dotnet
        #:package DotNetDo@*
        using DotNetDo;

        Console.WriteLine("Hello from {appName}");
        """;

    static void MakeExecutableIfUnix(string fileName)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                File.SetUnixFileMode(fileName,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
            catch
            {
                // ignored
            }
        }
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

    static bool IsValidAppName(string appName) =>
        !appName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
        && AppNameRegex().IsMatch(appName);

    [GeneratedRegex("^[A-Za-z0-9_.-]+$")]
    private static partial Regex AppNameRegex();
}