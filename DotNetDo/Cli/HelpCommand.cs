namespace DotNetDo.Cli;

static class HelpCommand
{
    public static int Run(string[] args)
    {
        if (args.Length == 2)
            return TaskScaffolding.IsValidName(args[1])
                ? TaskHelp.Show(args[1])
                : Fail(TaskScaffolding.InvalidNameMessage);

        Console.WriteLine("""
            Usage:
              do
              do :init
              do :new <name>
              do :help <name>
              do :help
              do <name> [args...]
            """);
        return 0;
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
