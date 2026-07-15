namespace DotNetDo;

/// <summary>Describes a project entry in a solution without loading MSBuild.</summary>
public sealed class ProjectInfo
{
    internal ProjectInfo(string solutionPath, AbsolutePath path)
    {
        SolutionPath = solutionPath;
        Path = path;
        Directory = path.Parent!;
    }

    /// <summary>The project's logical path inside the solution.</summary>
    public string SolutionPath { get; }
    /// <summary>The absolute filesystem path.</summary>
    public AbsolutePath Path { get; }
    /// <summary>The containing directory.</summary>
    public AbsolutePath Directory { get; }

    /// <summary>Loads and parses the referenced project or solution resource.</summary>
    public LoadedProject Load(IReadOnlyDictionary<string, string>? globalProperties = null)
    {
        try
        {
            return MSBuildLoader.Load(Path, globalProperties);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to load project '{SolutionPath}' at '{Path}'.", exception);
        }
    }
}
