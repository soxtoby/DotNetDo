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

        var plan = BuildPlan(catalog, taskName, Render(taskArgs));
        await RequiredParameterPreflight.Apply(catalog, plan);

        foreach (var invocation in plan)
        {
            var exitCode = await RunFile(catalog.ScriptsPath, invocation);
            if (exitCode != 0)
                return exitCode;
        }

        return 0;
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

    static List<RunInvocation> BuildPlan(TaskCatalog catalog, string taskName, string inheritedArguments)
    {
        var plan = new List<RunInvocation>();
        Add(taskName, inheritedArguments);
        return plan;

        void Add(string childTask, string arguments)
        {
            if (catalog.TryGetMetaTask(childTask, out var invocations))
            {
                foreach (var invocation in invocations)
                    Add(invocation.TaskName, Combine(arguments, invocation.Arguments));
            }
            else
            {
                plan.Add(new(childTask, arguments));
            }
        }
    }

    static async Task<int> RunFile(RelativePath scriptsPath, RunInvocation invocation)
    {
        var file = Do.RootDirectory / scriptsPath / $"{invocation.TaskName}.cs";
        var arguments = file.ToString().QuotedArgument();
        if (invocation.Arguments.Length != 0)
            arguments += $" -- {invocation.Arguments}";

        var startInfo = new ProcessStartInfo("dotnet", arguments) { UseShellExecute = false };
        foreach (var (name, value) in invocation.Environment)
            startInfo.Environment[name] = value;
        using var process = Process.Start(startInfo);

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

sealed class RunInvocation(string taskName, string arguments)
{
    public string TaskName { get; } = taskName;
    public string Arguments { get; set; } = arguments;
    public Dictionary<string, string> Environment { get; } = new(StringComparer.Ordinal);
}
