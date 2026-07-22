namespace DotNetDo.Cli;

static class InstallCommand
{
    public static async Task<int> Run(string[] args)
    {
        if (args.Length > 1)
            return Fail("Usage: dotnet do :install");

        var tools = WorkspaceConfiguration.Load(Do.RootDirectory).Tools;
        
        if (tools.None())
        {
            Console.WriteLine($"No tool requirements are declared in {WorkspaceConfiguration.FileName}.");
            return 0;
        }
        
        try
        {
            foreach (var tool in tools.Where(t => !t.IsAvailable))
                await tool;
        }
        catch (Exception exception) when (exception is ToolInstallException or ExecFailedException)
        {
            return Fail(exception.Message);
        }

        return 0;
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
