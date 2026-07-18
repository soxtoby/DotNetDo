namespace DotNetDo.Cli;

static class HelpCommand
{
    public static int Run(string[] args)
    {
        if (args.Length == 2)
            return TaskName.IsValid(args[1])
                ? TaskHelp.Show(args[1])
                : Fail(TaskName.InvalidMessage);

        Console.WriteLine("""
            Usage:
              dotnet do
              dotnet do :init
              dotnet do :new <name>
              dotnet do :help <name>
              dotnet do :help
              dotnet do <name> [args...]
            """);
        return 0;
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
