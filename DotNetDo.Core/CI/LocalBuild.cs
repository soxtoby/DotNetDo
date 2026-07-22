namespace DotNetDo;

public static partial class Do
{
    static readonly Lazy<bool> LocalBuild = new(() =>
        GitHubActions is null 
        && AzurePipelines is null 
        && !CIEnvironment.IsTrue("CI"));

    /// <summary>Whether the current process is running outside CI.</summary>
    public static bool IsLocalBuild => LocalBuild.Value;
}
