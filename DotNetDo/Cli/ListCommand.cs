namespace DotNetDo.Cli;

static class ListCommand
{
    public static int Run()
    {
        Console.WriteLine("Usage: dotnet do <task> [args...]");
        Console.WriteLine();
        Console.WriteLine("Tasks:");
        var tasks = TaskCatalog.Load().Tasks.ToArray();
        var nameWidth = tasks.Select(task => task.Name.Length).DefaultIfEmpty().Max();
        foreach (var task in tasks)
            Console.WriteLine(task.Description is null
                ? $"  {task.Name}"
                : $"  {task.Name.PadRight(nameWidth)}  {task.Description}");
        return 0;
    }
}
