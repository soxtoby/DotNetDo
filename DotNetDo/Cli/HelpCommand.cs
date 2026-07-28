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
              dotnet do
              dotnet do {{CliCommands.Init.Name}}
              dotnet do {{CliCommands.New.Name}} <name>
              dotnet do {{CliCommands.Install.Name}}
              dotnet do {{CliCommands.Completion.Name}} [pwsh|bash|zsh]
              dotnet do {{CliCommands.Completion.Name}} uninstall [pwsh|bash|zsh]
              dotnet do {{CliCommands.Update.Name}} [<package> | --all] [--prerelease]
              dotnet do {{CliCommands.Help.Name}} <name>
              dotnet do {{CliCommands.Help.Name}}
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
