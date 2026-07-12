using Microsoft.Build.Evaluation;

namespace DotNetDo;

/// <summary>Owns a live MSBuild project and its isolated project collection.</summary>
public sealed class LoadedProject : IDisposable
{
    readonly ProjectCollection _projects;

    internal LoadedProject(Project project, ProjectCollection projects)
    {
        Project = project;
        _projects = projects;
    }

    /// <summary>Gets or sets project.</summary>
    public Project Project { get; }

    /// <summary>Releases resources owned by this wrapper.</summary>
    public void Dispose()
    {
        _projects.UnloadAllProjects();
        _projects.Dispose();
    }
}
