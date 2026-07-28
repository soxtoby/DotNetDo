namespace DotNetDo.Cli;

static class CompleteCommand
{
    public static int Run(string[] args) => Run(args, Console.Out);

    internal static int Run(string[] args, TextWriter output)
    {
        try
        {
            if (!CompletionRequest.TryParse(args, out var request))
                return 0;

            foreach (var candidate in CompletionEngine.Complete(
                TaskCatalog.Load(),
                Do.RootDirectory,
                request.Arguments,
                request.ActiveArgumentIndex))
                output.WriteLine($"{candidate.Value}\t{candidate.Detail}");
        }
        catch
        {
            // Completion must never leak diagnostics into the shell.
        }
        return 0;
    }

    internal sealed record CompletionRequest(int ActiveArgumentIndex, string[] Arguments)
    {
        public static bool TryParse(string[] args, out CompletionRequest request)
        {
            // Shell adapters send: :complete <active token index> -- <executable> <arguments...>.
            if (args is [_, var activeText, "--", _, .. var arguments]
                && int.TryParse(activeText, out var activeTokenIndex)
                && activeTokenIndex > 0)
            {
                var activeArgumentIndex = activeTokenIndex - 1;
                if (activeArgumentIndex == arguments.Length)
                    arguments = [.. arguments, ""];
                if (activeArgumentIndex < arguments.Length)
                {
                    request = new(activeArgumentIndex, arguments);
                    return true;
                }
            }

            request = null!;
            return false;
        }
    }
}
