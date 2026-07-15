using Microsoft.Build.Evaluation;

namespace DotNetDo;

static class MSBuildLoader
{
    public static LoadedProject Load(string path, IReadOnlyDictionary<string, string>? globalProperties)
    {
        var properties = globalProperties is null
            ? null
            : new Dictionary<string, string>(globalProperties, StringComparer.OrdinalIgnoreCase);
        var projects = new ProjectCollection(properties);
        var project = projects.LoadProject(path);
        return new LoadedProject(project, projects);
    }
}
