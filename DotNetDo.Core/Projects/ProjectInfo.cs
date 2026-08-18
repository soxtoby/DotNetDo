using Microsoft.Build.Evaluation;

namespace DotNetDo;

/// <summary>Describes a project entry in a solution and provides access to its MSBuild evaluations.</summary>
public sealed class ProjectInfo
{
    readonly Lazy<Project> _project;

    internal ProjectInfo(string name, string solutionPath, AbsolutePath path)
    {
        Name = name;
        SolutionPath = solutionPath;
        Path = path;
        Directory = path.Parent;
        _project = new(() => Evaluate(() => MSBuildLoader.Load(Path)), LazyThreadSafetyMode.PublicationOnly);
    }

    /// <summary>The project name authored in the solution.</summary>
    public string Name { get; }
    /// <summary>The project's logical path inside the solution.</summary>
    public string SolutionPath { get; }
    /// <summary>The absolute filesystem path.</summary>
    public AbsolutePath Path { get; }
    /// <summary>The containing directory.</summary>
    public AbsolutePath Directory { get; }

    /// <summary>The default evaluated MSBuild project, loaded once after the first successful access.</summary>
    public Project Project => _project.Value;

    /// <summary>Returns the absolute project file path.</summary>
    public override string ToString() => Path;

    /// <summary>Returns the absolute project file path.</summary>
    public static implicit operator string(ProjectInfo project) => project.Path;

    /// <summary>Returns the evaluated MSBuild project for the supplied global properties.</summary>
    public Project Load(IReadOnlyDictionary<string, string> globalProperties)
    {
        ArgumentNullException.ThrowIfNull(globalProperties);
        return Evaluate(() => MSBuildLoader.Load(Path, globalProperties));
    }

    Project Evaluate(Func<Project> load)
    {
        try
        {
            return load();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to load project '{SolutionPath}' at '{Path}'.", exception);
        }
    }
}
