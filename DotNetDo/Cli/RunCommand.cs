using System.Diagnostics;

namespace DotNetDo.Cli;

static class RunCommand
{
    public static async Task<int> RunAsync(string taskName, string[] taskArgs)
    {
        if (!TaskScaffolding.IsValidName(taskName))
            return Fail(TaskScaffolding.InvalidNameMessage);

        var relativeFile = Do.ScriptsPath / $"{taskName}.cs";
        var file = Do.RootDirectory / relativeFile;
        if (!file.IsExistingFile)
            return Fail($"{relativeFile} does not exist.");

        var startInfo = new ProcessStartInfo("dotnet") { UseShellExecute = false };
        startInfo.ArgumentList.Add(file);
        if (taskArgs.Length > 0)
        {
            startInfo.ArgumentList.Add("--");
            foreach (var argument in taskArgs)
                startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);
        if (process is null)
            return Fail("Failed to start dotnet.");
        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
