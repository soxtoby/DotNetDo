using Microsoft.Build.Evaluation;

namespace DotNetDo;

static class MSBuildLoader
{
    public static Project Load(string path) => ProjectCollection.GlobalProjectCollection.LoadProject(path);

    public static Project Load(string path, IReadOnlyDictionary<string, string> globalProperties)
    {
        var properties = new Dictionary<string, string>(globalProperties, StringComparer.OrdinalIgnoreCase);
        return ProjectCollection.GlobalProjectCollection.LoadProject(path, properties, toolsVersion: null);
    }
}
