using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetDo;

public static partial class Tools
{
    /// <summary>Creates a GitVersion command whose await returns calculated version variables.</summary>
    public static GitVersionCommand GitVersion => new();
}

/// <summary>Calculates version variables using the manifest-owned GitVersion.Tool package.</summary>
public sealed record GitVersionCommand : PackageToolCommand<GitVersionResult>
{
    /// <summary>Creates the default GitVersion invocation with JSON output and round-trip commit dates.</summary>
    public GitVersionCommand() : base("GitVersion.Tool", "dotnet-gitversion")
    {
        SetArgument("output", "-output ", "json");
        SetArgument("commit-date-format", "-overrideconfig ", "commit-date-format=O");
    }

    /// <summary>The repository to inspect; the execution directory is used when omitted.</summary>
    public AbsolutePath? TargetPath { get; init; }
    /// <summary>The GitVersion configuration file.</summary>
    public AbsolutePath? Config { get => ParsePath(GetArgument("config")); init => SetPath("config", "-config ", value); }
    /// <summary>Whether remote fetching is disabled.</summary>
    public bool NoFetch { get => GetFlag("nofetch"); init => SetFlag("nofetch", "-nofetch", value); }
    /// <summary>Whether GitVersion's cache is disabled.</summary>
    public bool NoCache { get => GetFlag("nocache"); init => SetFlag("nocache", "-nocache", value); }
    /// <summary>Whether shallow repositories are accepted.</summary>
    public bool AllowShallow { get => GetFlag("allowshallow"); init => SetFlag("allowshallow", "-allowshallow", value); }
    /// <summary>Whether build-server repository normalization is disabled.</summary>
    public bool NoNormalize { get => GetFlag("nonormalize"); init => SetFlag("nonormalize", "-nonormalize", value); }
    /// <summary>Whether diagnostic processing is enabled.</summary>
    public bool Diagnostic { get => GetFlag("diag"); init => SetFlag("diag", "-diag", value); }
    /// <summary>The diagnostic log output file.</summary>
    public AbsolutePath? LogFile { get => ParsePath(GetArgument("logfile")); init => SetPath("logfile", "-l ", value); }
    /// <summary>The remote repository URL.</summary>
    public string? Url { get => GetArgument("url"); init => SetArgument("url", "-url ", value?.QuotedArgument()); }
    /// <summary>The remote branch name.</summary>
    public string? Branch { get => GetArgument("branch"); init => SetArgument("branch", "-b ", value?.QuotedArgument()); }
    /// <summary>The remote repository username.</summary>
    public string? Username { get => GetArgument("username"); init => SetArgument("username", "-u ", value?.QuotedArgument()); }
    /// <summary>The redacted remote repository password.</summary>
    public RequiredSecretParam? Password { get; init; }
    /// <summary>The remote commit to inspect.</summary>
    public string? Commit { get => GetArgument("commit"); init => SetArgument("commit", "-c ", value?.QuotedArgument()); }
    /// <summary>The directory used for a dynamically cloned remote repository.</summary>
    public AbsolutePath? DynamicRepositoryLocation { get => ParsePath(GetArgument("dynamic-repository-location")); init => SetPath("dynamic-repository-location", "-dynamicRepoLocation ", value); }

    /// <summary>GitVersion configuration overrides; commit-date-format is reserved by the result contract.</summary>
    public IReadOnlyDictionary<string, string> OverrideConfig
    {
        get;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Keys.Any(key => key.Equals("commit-date-format", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("commit-date-format is reserved by the GitVersion result contract.", nameof(value));
            field = new Dictionary<string, string>(value, StringComparer.OrdinalIgnoreCase);
            SetArgumentArray("override-config", "-overrideconfig ", value.Select(pair => $"{pair.Key}={pair.Value.QuotedArgument()}").ToArray(), " -overrideconfig ");
        }
    } = new Dictionary<string, string>();

    /// <summary>The package-tool prefix plus positional target and credential arguments.</summary>
    protected override string CommandPrefix
    {
        get
        {
            var command = base.CommandPrefix;
            if (TargetPath is not null)
                command += " " + TargetPath.QuotedArgument();
            if (Password is { } password)
                command += " -p " + password.Unwrap().QuotedArgument();
            return command;
        }
    }

    /// <inheritdoc />
    protected override GitVersionResult ReadResult(ExecResult result) => GitVersionResult.Parse(result);

    void SetPath(string key, string prefix, AbsolutePath? value) => SetArgument(key, prefix, value?.QuotedArgument());
    static AbsolutePath? ParsePath(string? value) => value is null ? null : AbsolutePath.Parse(value.Trim('"'));
}

/// <summary>The semantic version variables emitted by GitVersion.</summary>
public sealed record GitVersionResult
{
    internal static GitVersionResult Parse(ExecResult result)
    {
        try
        {
            var json = string.Join(Environment.NewLine, result.Output.Where(line => line.Type == OutputType.Out).Select(line => line.Message));
            return JsonSerializer.Deserialize<GitVersionResult>(json)
                ?? throw new JsonException("GitVersion returned no JSON object.");
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or FormatException)
        {
            throw new ToolOutputException(result, typeof(GitVersionResult), exception);
        }
    }
    /// <summary>The assembly file version.</summary>
    public string? AssemblySemFileVer { get; init; }
    /// <summary>The assembly version.</summary>
    public string? AssemblySemVer { get; init; }
    /// <summary>The checked-out branch name.</summary>
    public string? BranchName { get; init; }
    /// <summary>The build metadata, usually commits since the version source.</summary>
    public int? BuildMetaData { get; init; }
    /// <summary>The commit timestamp parsed from forced round-trip output.</summary>
    public DateTimeOffset CommitDate { get; init; }
    /// <summary>The deprecated count of commits since the version source.</summary>
    public int? CommitsSinceVersionSource { get; init; }
    /// <summary>The branch name with path separators replaced.</summary>
    public string? EscapedBranchName { get; init; }
    /// <summary>Build metadata including branch and commit identity.</summary>
    public string? FullBuildMetaData { get; init; }
    /// <summary>The complete SemVer 2.0 version.</summary>
    public string? FullSemVer { get; init; }
    /// <summary>The assembly informational version.</summary>
    public string? InformationalVersion { get; init; }
    /// <summary>The major version component.</summary>
    public int Major { get; init; }
    /// <summary>The joined major, minor, and patch components.</summary>
    public string? MajorMinorPatch { get; init; }
    /// <summary>The minor version component.</summary>
    public int Minor { get; init; }
    /// <summary>The patch version component.</summary>
    public int Patch { get; init; }
    /// <summary>The prerelease label without punctuation.</summary>
    public string? PreReleaseLabel { get; init; }
    /// <summary>The prerelease label prefixed with a dash.</summary>
    public string? PreReleaseLabelWithDash { get; init; }
    /// <summary>The prerelease sequence number.</summary>
    public int? PreReleaseNumber { get; init; }
    /// <summary>The prerelease label and sequence number.</summary>
    public string? PreReleaseTag { get; init; }
    /// <summary>The prerelease tag prefixed with a dash.</summary>
    public string? PreReleaseTagWithDash { get; init; }
    /// <summary>The semantic version excluding build metadata.</summary>
    public string? SemVer { get; init; }
    /// <summary>The complete commit SHA.</summary>
    public string? Sha { get; init; }
    /// <summary>The abbreviated commit SHA.</summary>
    public string? ShortSha { get; init; }
    /// <summary>The number of uncommitted working-tree changes.</summary>
    public int UncommittedChanges { get; init; }
    /// <summary>The number of commits since the version source.</summary>
    public int VersionSourceDistance { get; init; }
    /// <summary>The increment selected at the version source.</summary>
    public string? VersionSourceIncrement { get; init; }
    /// <summary>The semantic version at the version source.</summary>
    public string? VersionSourceSemVer { get; init; }
    /// <summary>The commit SHA of the version source.</summary>
    public string? VersionSourceSha { get; init; }
    /// <summary>The branch-weighted prerelease number.</summary>
    public int? WeightedPreReleaseNumber { get; init; }

    /// <summary>Additional variables emitted by newer or customized GitVersion versions.</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalVariables { get; init; } = [];
}
