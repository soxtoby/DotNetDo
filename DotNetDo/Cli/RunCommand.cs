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

        var plan = BuildPlan(catalog, taskName, TaskCommandLine.FromArguments(taskArgs));
        await RequiredParameterPreflight.Apply(catalog, plan);

        foreach (var invocation in plan)
        {
            var exitCode = await RunFile(catalog.ScriptsPath, invocation);
            if (exitCode != 0)
                return exitCode;
        }

        return 0;
    }

    static List<RunInvocation> BuildPlan(TaskCatalog catalog, string taskName, TaskCommandLine commandLine)
    {
        var plan = new List<RunInvocation>();
        Add(taskName, commandLine);
        return plan;

        void Add(string childTask, TaskCommandLine childCommandLine)
        {
            if (catalog.TryGetMetaTask(childTask, out var invocations))
            {
                foreach (var invocation in invocations)
                    Add(invocation.TaskName, childCommandLine.AppendFixed(invocation.CommandLine));
            }
            else
            {
                plan.Add(new(childTask, childCommandLine));
            }
        }
    }

    static async Task<int> RunFile(RelativePath scriptsPath, RunInvocation invocation)
    {
        var file = Do.RootDirectory / scriptsPath / $"{invocation.TaskName}.cs";
        var startInfo = new ProcessStartInfo("dotnet") { UseShellExecute = false };
        startInfo.ArgumentList.Add(file.ToString());
        startInfo.ArgumentList.Add("--");
        foreach (var argument in invocation.CommandLine.ToArguments())
            startInfo.ArgumentList.Add(argument);
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

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }

}

sealed class RunInvocation(string taskName, TaskCommandLine commandLine)
{
    public string TaskName { get; } = taskName;
    public TaskCommandLine CommandLine { get; set; } = commandLine;
    public Dictionary<string, string> Environment { get; } = new(StringComparer.Ordinal);
}
