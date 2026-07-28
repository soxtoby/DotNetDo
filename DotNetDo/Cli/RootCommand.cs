namespace DotNetDo.Cli;

static class RootCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args.Length == 0)
                return ListCommand.Run();

            if (CliCommands.TryGet(args[0], out var command))
                return await command.Run(args);

            return args[0].StartsWith(':')
                ? Fail($"Unknown command '{args[0]}'.")
                : await RunCommand.RunTask(args[0], args[1..]);
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
