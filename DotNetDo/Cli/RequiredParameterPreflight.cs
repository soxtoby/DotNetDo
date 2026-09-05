namespace DotNetDo.Cli;

static class RequiredParameterPreflight
{
    public static async Task Apply(TaskCatalog catalog, IReadOnlyList<RunInvocation> plan)
    {
        if (!Do.IsLocalBuild || Console.IsInputRedirected || Console.IsOutputRedirected)
            return;

        var occurrences = plan
            .SelectMany(invocation =>
                TaskHelp.Discover(Do.RootDirectory / catalog.ScriptsPath / $"{invocation.TaskName}.cs")
                    .Where(parameter => parameter.Required)
                    .Select(parameter => new ParameterOccurrence(invocation, parameter)))
            .Where(occurrence => Configuration(occurrence.Invocation)[occurrence.Parameter.Name] is null)
            .ToArray();

        var taskNames = occurrences.Select(occurrence => occurrence.Invocation.TaskName).Distinct(StringComparer.Ordinal);
        var results = await Task.WhenAll(taskNames
            .Select(async taskName => (TaskName: taskName, Result: await LoadUserSecrets(catalog.ScriptsPath, taskName))));
        var secrets = results.ToDictionary(result => result.TaskName, result => result.Result, StringComparer.Ordinal);
        foreach (var occurrence in occurrences)
        {
            var result = secrets[occurrence.Invocation.TaskName];
            occurrence.IsMissing = result.Inspected
                && TaskParameterConfiguration.Create(occurrence.Invocation.CommandLine.Parameters, result.UserSecretsId)[occurrence.Parameter.Name] is null;
        }

        foreach (var group in occurrences
            .Where(occurrence => occurrence.IsMissing)
            .GroupBy(occurrence => occurrence.Parameter.Name, StringComparer.Ordinal))
        {
            var values = group.ToArray();
            var secret = values.Any(value => value.Parameter.Secret);
            var parameter = CompatibleParameter(values);
            if (ParameterPrompt.TryRead(
                group.Key,
                parameter?.Type,
                parameter?.Description,
                secret,
                parameter?.Values ?? [],
                out var answer))
            {
                foreach (var occurrence in values)
                {
                    if (secret)
                        occurrence.Invocation.Environment[$"DOTNETDO_{group.Key}"] = answer;
                    else
                        occurrence.Invocation.CommandLine = occurrence.Invocation.CommandLine.AppendParameters($"--{group.Key}", answer);
                }
            }
        }
    }

    static TaskHelp.TaskParameter? CompatibleParameter(ParameterOccurrence[] occurrences)
    {
        var first = occurrences[0].Parameter;
        return occurrences.All(occurrence => occurrence.Parameter.Type == first.Type
                && occurrence.Parameter.Secret == first.Secret
                && occurrence.Parameter.Values.SequenceEqual(first.Values, StringComparer.Ordinal))
            ? first
            : null;
    }

    static TaskParameterConfiguration Configuration(RunInvocation invocation) =>
        TaskParameterConfiguration.Create(invocation.CommandLine.Parameters);

    static async Task<UserSecretsResult> LoadUserSecrets(RelativePath scriptsPath, string taskName)
    {
        var file = Do.RootDirectory / scriptsPath / $"{taskName}.cs";
        var result = await Do.Exec(
            $"dotnet build {file.QuotedArgument()} -getProperty:UserSecretsId --no-restore",
            new ExecOptions
                {
                    WorkingDirectory = Do.RootDirectory,
                    Log = ExecLog.None,
                    LogCommand = false
                }).Completed;
        var id = result.OutputLines().LastOrDefault()?.Trim();
        return result.ExitCode == 0 && !id.IsNullOrWhiteSpace()
            ? new(true, id)
            : new(false, null);
    }

    sealed class ParameterOccurrence(RunInvocation invocation, TaskHelp.TaskParameter parameter)
    {
        public RunInvocation Invocation { get; } = invocation;
        public TaskHelp.TaskParameter Parameter { get; } = parameter;
        public bool IsMissing { get; set; }
    }

    readonly record struct UserSecretsResult(bool Inspected, string? UserSecretsId);
}
