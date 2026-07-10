namespace DotNetDo;

public static class BuildEnvironment
{
    public static bool IsGitHubActions => Environment.GetEnvironmentVariable("GITHUB_ACTIONS")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    public static bool IsAzurePipelines => Environment.GetEnvironmentVariable("TF_BUILD")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    public static bool IsCI => IsGitHubActions || IsAzurePipelines;
}