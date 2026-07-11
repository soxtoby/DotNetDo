namespace DotNetDo;

public sealed class ProjectInfo
{
    internal ProjectInfo(string solutionPath, AbsolutePath path)
    {
        SolutionPath = solutionPath;
        Path = path;
        Directory = path.Parent!;
    }

    public string SolutionPath { get; }
    public AbsolutePath Path { get; }
    public AbsolutePath Directory { get; }

    public LoadedProject Load(IReadOnlyDictionary<string, string>? globalProperties = null)
    {
        try
        {
            return MSBuildLoader.Load(Path.NativePath, globalProperties);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to load project '{SolutionPath}' at '{Path}'.", exception);
        }
    }
}
