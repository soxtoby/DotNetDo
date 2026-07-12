namespace DotNetDo;

/// <summary>Detects supported continuous-integration hosts from their environment variables.</summary>
public static class BuildEnvironment
{
    /// <summary>Is this running in GitHub Actions.</summary>
    public static bool IsGitHubActions => Environment.GetEnvironmentVariable("GITHUB_ACTIONS")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    /// <summary>Is this running in Azure Pipelines.</summary>
    public static bool IsAzurePipelines => Environment.GetEnvironmentVariable("TF_BUILD")?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    /// <summary>Is this running in one of the supported CI environments.</summary>
    public static bool IsCI => IsGitHubActions || IsAzurePipelines;
}