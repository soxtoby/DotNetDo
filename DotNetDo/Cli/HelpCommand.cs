namespace DotNetDo.Cli;

static class HelpCommand
{
    public static int Run(string[] args)
    {
        if (args.Length == 2)
            return TaskName.IsValid(args[1])
                ? TaskHelp.Show(args[1])
                : Fail(TaskName.InvalidMessage);

        Console.WriteLine($$"""
            Usage:
              dotnetdo {{CliCommands.Init.Name}}
              dotnetdo {{CliCommands.Completion.Name}} [pwsh|bash|zsh]
              dotnetdo {{CliCommands.Completion.Name}} uninstall [pwsh|bash|zsh]
              ./do
              ./do {{CliCommands.New.Name}} <name>
              ./do {{CliCommands.Rename.Name}} <old-name> <new-name>
              ./do {{CliCommands.Install.Name}}
              ./do {{CliCommands.Update.Name}} [<package> | --all] [--prerelease]
              ./do {{CliCommands.Help.Name}} <name>
              ./do {{CliCommands.Help.Name}}
              ./do <name> [args...]
            """);
        return 0;
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
