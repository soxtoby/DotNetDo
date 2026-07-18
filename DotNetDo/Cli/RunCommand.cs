using System.Diagnostics;

namespace DotNetDo.Cli;

static class RunCommand
{
    public static async Task<int> RunTask(string taskName, string[] taskArgs)
    {
        if (!TaskName.IsValid(taskName))
            return Fail(TaskName.InvalidMessage);

        var catalog = TaskCatalog.Load();
        if (!catalog.Contains(taskName))
            return Fail($"Task '{taskName}' does not exist.");

        return await RunTask(
            catalog,
            taskName,
            Render(taskArgs),
            (childTask, arguments) => RunFile(catalog.ScriptsPath, childTask, arguments));
    }

    internal static async Task<int> RunTask(
        TaskCatalog catalog,
        string taskName,
        string inheritedArguments,
        Func<string, string, Task<int>> runTaskFile)
    {
        if (catalog.TryGetMetaTask(taskName, out var invocations))
        {
            foreach (var invocation in invocations)
            {
                var childArguments = Combine(inheritedArguments, invocation.Arguments);
                var exitCode = await RunTask(catalog, invocation.TaskName, childArguments, runTaskFile);
                if (exitCode != 0)
                    return exitCode;
            }

            return 0;
        }

        return await runTaskFile(taskName, inheritedArguments);
    }

    static async Task<int> RunFile(RelativePath scriptsPath, string taskName, string taskArguments)
    {
        var file = Do.RootDirectory / scriptsPath / $"{taskName}.cs";
        var arguments = file.ToString().QuotedArgument();
        if (taskArguments.Length != 0)
            arguments += $" -- {taskArguments}";

        using var process = Process.Start(new ProcessStartInfo("dotnet", arguments) { UseShellExecute = false });

        if (process is null)
        {
            return Fail("Failed to start dotnet.");
        }
        else
        {
            await process.WaitForExitAsync();
            return process.ExitCode;
        }
    }

    static string Render(IEnumerable<string> arguments) =>
        string.Join(" ", arguments.Select(argument => argument.QuotedArgument()));

    static string Combine(string inherited, string fixedArguments) =>
        $"{inherited} {fixedArguments}".Trim();

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
