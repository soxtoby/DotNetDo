namespace DotNetDo.Cli;

static class ListCommand
{
    public static int Run()
    {
        Console.WriteLine("Usage: dotnet do <task> [args...]");
        Console.WriteLine();
        Console.WriteLine("Tasks:");
        foreach (var task in TaskCatalog.Load().Names)
            Console.WriteLine($"  {task}");
        return 0;
    }
}
