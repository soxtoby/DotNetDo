using System.Diagnostics.CodeAnalysis;

namespace DotNetDo.Cli;

static class CliCommands
{
    public static readonly CliCommand Init = new(":init", "Initialize a workspace", args => Task.FromResult(InitCommand.Run(args)));
    public static readonly CliCommand New = new(":new", "Create a task", args => Task.FromResult(NewCommand.Run(args)));
    public static readonly CliCommand Install = new(":install", "Install workspace tool requirements", InstallCommand.Run);
    public static readonly CliCommand Update = new(":update", "Update DotNetDo and task package pins", UpdateCommand.Run);
    public static readonly CliCommand Completion = new(":completion", "Install shell completion", args => Task.FromResult(CompletionCommand.Run(args)));
    public static readonly CliCommand Help = new(":help", "Show task or command help", args => Task.FromResult(HelpCommand.Run(args)));
    public static readonly CliCommand Complete = new(":complete", "", args => Task.FromResult(CompleteCommand.Run(args)), Hidden: true);

    static readonly CliCommand[] All = [Init, New, Install, Update, Completion, Help, Complete];

    public static IEnumerable<CliCommand> Visible => All.Where(command => !command.Hidden);

    public static bool TryGet(string name, [NotNullWhen(true)] out CliCommand? command)
        => (command = All.FirstOrDefault(candidate => candidate.Name == name)) is not null;
}

sealed record CliCommand(
    string Name,
    string Description,
    Func<string[], Task<int>> Run,
    bool Hidden = false);
