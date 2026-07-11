using Microsoft.Build.Evaluation;

namespace DotNetDo;

public sealed class LoadedProject : IDisposable
{
    readonly ProjectCollection _projects;

    internal LoadedProject(Project project, ProjectCollection projects)
    {
        Project = project;
        _projects = projects;
    }

    public Project Project { get; }

    public void Dispose()
    {
        _projects.UnloadAllProjects();
        _projects.Dispose();
    }
}
