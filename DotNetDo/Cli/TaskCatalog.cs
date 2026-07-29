namespace DotNetDo.Cli;

sealed class TaskCatalog
{
    readonly IReadOnlyDictionary<string, TaskDefinition> _csharpTasks;
    readonly IReadOnlyDictionary<string, TaskInvocation[]> _metaTasks;

    TaskCatalog(RelativePath scriptsPath, IReadOnlyDictionary<string, TaskDefinition> csharpTasks, IReadOnlyDictionary<string, TaskInvocation[]> metaTasks)
    {
        ScriptsPath = scriptsPath;
        _csharpTasks = csharpTasks;
        _metaTasks = metaTasks;
    }

    public RelativePath ScriptsPath { get; }
    public IEnumerable<TaskDefinition> Tasks =>
        _csharpTasks.Values
            .Concat(_metaTasks.Keys.Select(name => new TaskDefinition(name, null)))
            .OrderBy(task => task.Name, StringComparer.OrdinalIgnoreCase);

    public static TaskCatalog Load()
    {
        var rootDirectory = Do.RootDirectory;
        return Load(rootDirectory, WorkspaceConfiguration.Load(rootDirectory));
    }

    internal static TaskCatalog Load(AbsolutePath rootDirectory, RelativePath scriptsPath)
        => Load(rootDirectory, WorkspaceConfiguration.Load(rootDirectory) with { ScriptsPath = scriptsPath });

    static TaskCatalog Load(AbsolutePath rootDirectory, WorkspaceConfiguration configuration)
    {
        var scriptsPath = configuration.ScriptsPath;
        var scriptsDirectory = rootDirectory / scriptsPath;
        var csharpTasks = scriptsDirectory.IsExistingDirectory
            ? scriptsDirectory.GlobFiles("*.cs")
                .Where(file => file.NameWithoutExtension is { } name && TaskName.IsValid(name))
                .ToDictionary(
                    file => file.NameWithoutExtension!,
                    file => new TaskDefinition(file.NameWithoutExtension!, TaskMetadata.DiscoverDescription(file)),
                    StringComparer.Ordinal)
            : new Dictionary<string, TaskDefinition>(StringComparer.Ordinal);

        foreach (var name in configuration.MetaTasks.Keys)
            if (csharpTasks.ContainsKey(name))
                throw new DotNetDoConfigurationException($"Task '{name}' is defined by both '{scriptsPath / $"{name}.cs"}' and the 'tasks' table.");

        var metaTasks = configuration.MetaTasks.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.Select(value => TaskInvocation.Parse(pair.Key, value)).ToArray(),
            StringComparer.Ordinal);

        ValidateReferences(csharpTasks, metaTasks);
        ValidateCycles(metaTasks);
        return new(scriptsPath, csharpTasks, metaTasks);
    }

    public bool Contains(string name) => _csharpTasks.ContainsKey(name) || _metaTasks.ContainsKey(name);

    public bool TryGetMetaTask(string name, out TaskInvocation[] invocations) =>
        _metaTasks.TryGetValue(name, out invocations!);

    public IEnumerable<string> LeafTasks(string name)
    {
        if (!_metaTasks.TryGetValue(name, out var invocations))
            return _csharpTasks.ContainsKey(name) ? [name] : [];

        return invocations
            .SelectMany(invocation => LeafTasks(invocation.TaskName))
            .Distinct(StringComparer.Ordinal);
    }

    static void ValidateReferences(IReadOnlyDictionary<string, TaskDefinition> csharpTasks, IReadOnlyDictionary<string, TaskInvocation[]> metaTasks)
    {
        foreach (var (owner, invocations) in metaTasks)
            foreach (var invocation in invocations)
                if (!csharpTasks.ContainsKey(invocation.TaskName) && !metaTasks.ContainsKey(invocation.TaskName))
                    throw new DotNetDoConfigurationException($"Meta-task '{owner}' invokes unknown task '{invocation.TaskName}'.");
    }

    static void ValidateCycles(IReadOnlyDictionary<string, TaskInvocation[]> metaTasks)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var path = new List<string>();

        foreach (var task in metaTasks.Keys)
            Visit(task);

        void Visit(string task)
        {
            if (!visited.Contains(task))
            {
                if (visiting.Add(task))
                {
                    path.Add(task);
                    foreach (var invocation in metaTasks[task])
                        if (metaTasks.ContainsKey(invocation.TaskName))
                            Visit(invocation.TaskName);
                    path.RemoveAt(path.Count - 1);
                    visiting.Remove(task);
                    visited.Add(task);
                }
                else
                {
                    var cycleStart = path.IndexOf(task);
                    var cycle = path.Skip(cycleStart).Append(task);
                    throw new DotNetDoConfigurationException($"Meta-task cycle: {string.Join(" -> ", cycle)}.");
                }
            }
        }
    }
}

sealed record TaskDefinition(string Name, string? Description);

sealed record TaskInvocation(string TaskName, string Arguments)
{
    public static TaskInvocation Parse(string owner, string value)
    {
        var invocation = value.Trim();
        var separator = invocation.IndexOfAny([' ', '\t', '\r', '\n']);
        var taskName = separator < 0 ? invocation : invocation[..separator];
        var arguments = separator < 0 ? "" : invocation[separator..].TrimStart();

        return DotNetDo.TaskName.IsValid(taskName) 
            ? new(taskName, arguments) 
            : throw new DotNetDoConfigurationException($"Meta-task '{owner}' contains invalid invocation '{value}'. {DotNetDo.TaskName.InvalidMessage}");
    }
}
