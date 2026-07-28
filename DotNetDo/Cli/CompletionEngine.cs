namespace DotNetDo.Cli;

static class CompletionEngine
{
    public static CompletionCandidate[] Complete(
        TaskCatalog catalog,
        AbsolutePath root,
        IReadOnlyList<string> arguments,
        int activeIndex)
    {
        if (arguments.Count == 0 || activeIndex < 0 || activeIndex >= arguments.Count)
            return [];

        if (activeIndex == 0)
            return CompleteCommands(catalog, arguments[activeIndex]);

        var command = arguments[0];
        return command switch
            {
                _ when command == CliCommands.Help.Name => CompleteTaskNames(catalog, arguments[activeIndex]),
                _ when command == CliCommands.Update.Name => CompleteUpdate(catalog, root, arguments, activeIndex),
                _ when command == CliCommands.Completion.Name => CompleteCompletion(arguments, activeIndex),
                _ when command.StartsWith(':') => [],
                _ when catalog.Contains(command) => CompleteTask(catalog, root, command, arguments, activeIndex),
                _ => [],
            };
    }

    static CompletionCandidate[] CompleteCommands(TaskCatalog catalog, string prefix) =>
        [
            .. Match(catalog.Names.Select(name => new CompletionCandidate(name, "Task")), prefix),
            .. Match(
                CliCommands.Visible
                    .Where(command => command != CliCommands.Completion)
                    .Select(command => new CompletionCandidate(command.Name, command.Description)),
                prefix),
            .. Match(
                [new(CliCommands.Completion.Name, CliCommands.Completion.Description)],
                prefix),
        ];

    static CompletionCandidate[] CompleteTaskNames(TaskCatalog catalog, string prefix) =>
        Match(catalog.Names.Select(name => new CompletionCandidate(name, "Task")), prefix);

    static CompletionCandidate[] CompleteUpdate(TaskCatalog catalog, AbsolutePath root, IReadOnlyList<string> tokens, int activeIndex)
    {
        var candidates = new List<CompletionCandidate>
            {
                new("--all", "Update every pinned package"),
                new("--prerelease", "Include prerelease versions"),
            };
        candidates.AddRange(UpdateCommand.PinnedPackages(root / catalog.ScriptsPath)
            .Select(package => new CompletionCandidate(package, "Pinned package")));
        return Match(candidates.Where(candidate => !Used(tokens, activeIndex, candidate.Value)), tokens[activeIndex]);
    }

    static CompletionCandidate[] CompleteCompletion(IReadOnlyList<string> tokens, int activeIndex)
    {
        var candidates = activeIndex switch
            {
                1 => Shells().Prepend(new("uninstall", "Remove shell completion")),
                2 when tokens[1] == "uninstall" => Shells(),
                _ => [],
            };
        return Match(candidates, tokens[activeIndex]);
    }

    static IEnumerable<CompletionCandidate> Shells()
    {
        yield return new("bash", "Bash");
        yield return new("pwsh", "PowerShell");
        yield return new("zsh", "Zsh");
    }

    static CompletionCandidate[] CompleteTask(
        TaskCatalog catalog,
        AbsolutePath root,
        string taskName,
        IReadOnlyList<string> arguments,
        int activeIndex)
    {
        var parameters = Parameters(catalog, root, taskName);
        return CompleteAttachedValue(parameters, arguments[activeIndex])
            ?? CompleteSeparateValue(parameters, arguments, activeIndex)
            ?? CompleteParameterNames(parameters, arguments, activeIndex);
    }

    static CompletionCandidate[]? CompleteAttachedValue(CompletionParameter[] parameters, string current)
    {
        var equals = current.IndexOf('=');
        if (equals <= 2 || !current.StartsWith("--", StringComparison.Ordinal))
            return null;

        var option = current[2..equals];
        var parameter = parameters.FirstOrDefault(item => item.Name == option);
        return parameter is null
            ? []
            : Match(
                parameter.Values.Select(value => new CompletionCandidate($"--{option}={value}", parameter.Detail)),
                current);
    }

    static CompletionCandidate[]? CompleteSeparateValue(
        CompletionParameter[] parameters,
        IReadOnlyList<string> arguments,
        int activeIndex)
    {
        if (activeIndex <= 1 || !arguments[activeIndex - 1].StartsWith("--", StringComparison.Ordinal))
            return null;

        var option = arguments[activeIndex - 1][2..];
        var parameter = parameters.FirstOrDefault(item => item.Name == option);
        return parameter is null
            ? null
            : Match(parameter.Values.Select(value => new CompletionCandidate(value, parameter.Detail)), arguments[activeIndex]);
    }

    static CompletionCandidate[] CompleteParameterNames(
        CompletionParameter[] parameters,
        IReadOnlyList<string> arguments,
        int activeIndex) =>
        Match(
            parameters
                .Where(parameter => !Used(arguments, activeIndex, $"--{parameter.Name}"))
                .Select(parameter => new CompletionCandidate($"--{parameter.Name}", parameter.Detail)),
            arguments[activeIndex]);

    static CompletionParameter[] Parameters(TaskCatalog catalog, AbsolutePath root, string taskName)
    {
        var groups = catalog.LeafTasks(taskName)
            .SelectMany(leaf =>
                {
                    var file = root / catalog.ScriptsPath / $"{leaf}.cs";
                    return file.IsExistingFile ? TaskHelp.Discover(file).Select(parameter => (Leaf: leaf, Parameter: parameter)) : [];
                })
            .GroupBy(item => item.Parameter.Name, StringComparer.Ordinal);

        return
            [
                .. groups.Select(group =>
                    {
                        var parameters = group.Select(item => item.Parameter).ToArray();
                        var first = parameters[0];
                        var identical = parameters.All(first.HasSameCompletionMetadata);
                        return new CompletionParameter(
                            group.Key,
                            identical ? Detail(first) : null,
                            identical ? first.Values : []);
                    })
            ];
    }

    static string Detail(TaskHelp.TaskParameter parameter)
    {
        var type = parameter.Secret ? "secret string" : parameter.Type;
        if (parameter.Required)
            type += " (required)";
        return string.IsNullOrWhiteSpace(parameter.Description)
            ? type
            : $"{type} — {Sanitize(parameter.Description)}";
    }

    static string Sanitize(string value) => value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    static bool Used(IReadOnlyList<string> tokens, int activeIndex, string candidate) =>
        tokens.Take(activeIndex).Any(token =>
            token.Equals(candidate, StringComparison.Ordinal)
            || token.StartsWith(candidate + "=", StringComparison.Ordinal));

    static CompletionCandidate[] Match(IEnumerable<CompletionCandidate> candidates, string prefix) =>
        [
            .. candidates
                .Where(candidate => candidate.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .DistinctBy(candidate => candidate.Value, StringComparer.Ordinal)
                .OrderBy(candidate => candidate.Value, StringComparer.OrdinalIgnoreCase)
        ];

    sealed record CompletionParameter(string Name, string? Detail, IReadOnlyList<string> Values);
}

readonly record struct CompletionCandidate(string Value, string? Detail);
