namespace DotNetDo.Cli;

static class RootCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length == 0)
                return ListCommand.Run();

            return args[0] switch
            {
                ":init" => InitCommand.Run(args),
                ":new" => NewCommand.Run(args),
                ":install" => await InstallCommand.Run(args),
                ":help" => HelpCommand.Run(args),
                var command when command.StartsWith(':') => Fail($"Unknown command '{command}'."),
                var taskName => await RunCommand.RunTask(taskName, args[1..])
            };
        }
        catch (DotNetDoConfigurationException exception)
        {
            return Fail(exception.Message);
        }
    }

    static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
