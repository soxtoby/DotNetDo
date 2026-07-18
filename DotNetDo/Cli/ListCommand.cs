namespace DotNetDo.Cli;

static class ListCommand
{
    public static int Run()
    {
        foreach (var task in TaskCatalog.Load().Names)
            Console.WriteLine(task);
        return 0;
    }
}
