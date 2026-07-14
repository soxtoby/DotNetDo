using System.Text;

namespace DotNetDo;

public static partial class Do
{
    static readonly Lazy<GitHubActions?> GitHubActionsInstance = new(() =>
        CIEnvironment.IsTrue("GITHUB_ACTIONS") ? new GitHubActions() : null);

    /// <summary>The active GitHub Actions runner, or <see langword="null"/> outside GitHub Actions.</summary>
    public static GitHubActions? GitHubActions => GitHubActionsInstance.Value;
}

/// <summary>Exposes GitHub Actions workflow commands and runner metadata.</summary>
public sealed class GitHubActions
{
    readonly Lock _gate = new();

    internal GitHubActions()
    {
        Action = new();
        Event = new();
        Files = new();
        Repository = new();
        Run = new();
        Runner = new();
        Workflow = new();
    }

    /// <summary>Gets or configures the provider value for Action.</summary>
    public GitHubActionMetadata Action { get; }
    /// <summary>Gets or configures the provider value for Event.</summary>
    public GitHubEventMetadata Event { get; }
    /// <summary>Gets or configures the provider value for Files.</summary>
    public GitHubCommandFileMetadata Files { get; }
    /// <summary>Gets or configures the provider value for Repository.</summary>
    public GitHubRepositoryMetadata Repository { get; }
    /// <summary>Gets or configures the provider value for Run.</summary>
    public GitHubRunMetadata Run { get; }
    /// <summary>Gets or configures the provider value for Runner.</summary>
    public GitHubRunnerMetadata Runner { get; }
    /// <summary>Gets or configures the provider value for Workflow.</summary>
    public GitHubWorkflowMetadata Workflow { get; }

    /// <summary>Emits the provider's Debug command immediately.</summary>
    public void Debug(string message) => Command("debug", message);
    /// <summary>Emits the provider's Notice command immediately.</summary>
    public void Notice(string message, GitHubAnnotation? annotation = null) => Annotation("notice", message, annotation);
    /// <summary>Emits the provider's Warning command immediately.</summary>
    public void Warning(string message, GitHubAnnotation? annotation = null) => Annotation("warning", message, annotation);
    /// <summary>Emits the provider's Error command immediately.</summary>
    public void Error(string message, GitHubAnnotation? annotation = null) => Annotation("error", message, annotation);
    /// <summary>Emits the provider's StartGroup command immediately.</summary>
    public void StartGroup(string title) => Command("group", title);
    /// <summary>Emits the provider's EndGroup command immediately.</summary>
    public void EndGroup() => Command("endgroup", "");
    /// <summary>Emits the provider's AddMask command immediately.</summary>
    public void AddMask(string value) => Command("add-mask", value);
    /// <summary>Emits the provider's Echo command immediately.</summary>
    public void Echo(bool enabled) => Command("echo", enabled ? "on" : "off");
    /// <summary>Emits the provider's StopCommands command immediately.</summary>
    public void StopCommands(string token) { Required(token); Write($"::stop-commands::{EscapeData(token)}"); }
    /// <summary>Emits the provider's StartCommands command immediately.</summary>
    public void StartCommands(string token) { Required(token); Write($"::{EscapeData(token)}::"); }

    /// <summary>Emits the provider's SetEnvironmentVariable command immediately.</summary>
    public void SetEnvironmentVariable(string name, string value) => AppendKeyValue("GITHUB_ENV", name, value);
    /// <summary>Emits the provider's SetOutput command immediately.</summary>
    public void SetOutput(string name, string value) => AppendKeyValue("GITHUB_OUTPUT", name, value);
    /// <summary>Emits the provider's SaveState command immediately.</summary>
    public void SaveState(string name, string value) => AppendKeyValue("GITHUB_STATE", name, value);
    /// <summary>Emits the provider's AddPath command immediately.</summary>
    public void AddPath(AbsolutePath path) => Append("GITHUB_PATH", path + Environment.NewLine);
    /// <summary>Emits the provider's AppendSummary command immediately.</summary>
    public void AppendSummary(string markdown) => Append("GITHUB_STEP_SUMMARY", markdown + Environment.NewLine);
    /// <summary>Emits the provider's OverwriteSummary command immediately.</summary>
    public void OverwriteSummary(string markdown) => WriteFile("GITHUB_STEP_SUMMARY", markdown + Environment.NewLine);
    /// <summary>Emits the provider's ClearSummary command immediately.</summary>
    public void ClearSummary() => WriteFile("GITHUB_STEP_SUMMARY", "");

    void Annotation(string command, string message, GitHubAnnotation? annotation)
    {
        var properties = annotation is null ? "" : Properties(
            ("title", annotation.Title),
            ("file", annotation.File?.UnixPath),
            ("line", annotation.Line?.ToString()),
            ("endLine", annotation.EndLine?.ToString()),
            ("col", annotation.Column?.ToString()),
            ("endColumn", annotation.EndColumn?.ToString()));
        Command(command, message, properties);
    }

    void Command(string command, string message, string properties = "")
    {
        ArgumentNullException.ThrowIfNull(message);
        Write($"::{command}{(properties.Length == 0 ? "" : " " + properties)}::{EscapeData(message)}");
    }

    void AppendKeyValue(string variable, string name, string value)
    {
        Required(name);
        ArgumentNullException.ThrowIfNull(value);
        var delimiter = $"dotnetdo_{Guid.NewGuid():N}";
        Append(variable, $"{name}<<{delimiter}{Environment.NewLine}{value}{Environment.NewLine}{delimiter}{Environment.NewLine}");
    }

    void Append(string variable, string content)
    {
        var path = RequiredEnvironmentFile(variable);
        lock (_gate) File.AppendAllText(path, content, new UTF8Encoding(false));
    }

    void WriteFile(string variable, string content)
    {
        var path = RequiredEnvironmentFile(variable);
        lock (_gate) File.WriteAllText(path, content, new UTF8Encoding(false));
    }

    void Write(string value)
    {
        lock (_gate) Console.WriteLine(value);
    }

    static string RequiredEnvironmentFile(string variable) =>
        CIEnvironment.String(variable) is { Length: > 0 } path
            ? path
            : throw new InvalidOperationException($"GitHub Actions did not provide {variable}.");

    static string Properties(params (string Name, string? Value)[] values) =>
        string.Join(',', values.Where(value => value.Value is not null).Select(value => $"{value.Name}={EscapeProperty(value.Value!)}"));

    static string EscapeData(string value) => value.Replace("%", "%25").Replace("\r", "%0D").Replace("\n", "%0A");
    static string EscapeProperty(string value) => EscapeData(value).Replace(":", "%3A").Replace(",", "%2C");
    static void Required(string value) => ArgumentException.ThrowIfNullOrWhiteSpace(value);
}

/// <summary>Represents provider data for GitHubAnnotation.</summary>
public sealed record GitHubAnnotation
{
    /// <summary>Gets or configures the provider value for Title.</summary>
    public string? Title { get; init; }
    /// <summary>Gets or configures the provider value for File.</summary>
    public RelativePath? File { get; init; }
    /// <summary>Gets or configures the provider value for Line.</summary>
    public long? Line { get; init; }
    /// <summary>Gets or configures the provider value for EndLine.</summary>
    public long? EndLine { get; init; }
    /// <summary>Gets or configures the provider value for Column.</summary>
    public long? Column { get; init; }
    /// <summary>Gets or configures the provider value for EndColumn.</summary>
    public long? EndColumn { get; init; }
}

/// <summary>Represents provider data for GitHubActionMetadata.</summary>
public sealed record GitHubActionMetadata
{
    internal GitHubActionMetadata() { }
    /// <summary>Gets or configures the provider value for Name.</summary>
    public string? Name { get; } = CIEnvironment.String("GITHUB_ACTION");
    /// <summary>Gets or configures the provider value for Repository.</summary>
    public string? Repository { get; } = CIEnvironment.String("GITHUB_ACTION_REPOSITORY");
    /// <summary>Gets or configures the provider value for Reference.</summary>
    public string? Reference { get; } = CIEnvironment.String("GITHUB_ACTION_REF");
    /// <summary>Gets or configures the provider value for Path.</summary>
    public AbsolutePath? Path { get; } = CIEnvironment.Path("GITHUB_ACTION_PATH");
}

/// <summary>Represents provider data for GitHubEventMetadata.</summary>
public sealed record GitHubEventMetadata
{
    internal GitHubEventMetadata() { }
    /// <summary>Gets or configures the provider value for Name.</summary>
    public string? Name { get; } = CIEnvironment.String("GITHUB_EVENT_NAME");
    /// <summary>Gets or configures the provider value for Path.</summary>
    public AbsolutePath? Path { get; } = CIEnvironment.Path("GITHUB_EVENT_PATH");
    /// <summary>Gets or configures the provider value for Actor.</summary>
    public string? Actor { get; } = CIEnvironment.String("GITHUB_ACTOR");
    /// <summary>Gets or configures the provider value for ActorId.</summary>
    public long? ActorId { get; } = CIEnvironment.Long("GITHUB_ACTOR_ID");
    /// <summary>Gets or configures the provider value for TriggeringActor.</summary>
    public string? TriggeringActor { get; } = CIEnvironment.String("GITHUB_TRIGGERING_ACTOR");
}

/// <summary>Represents provider data for GitHubRepositoryMetadata.</summary>
public sealed record GitHubRepositoryMetadata
{
    internal GitHubRepositoryMetadata() { }
    /// <summary>Gets or configures the provider value for Name.</summary>
    public string? Name { get; } = CIEnvironment.String("GITHUB_REPOSITORY");
    /// <summary>Gets or configures the provider value for Id.</summary>
    public long? Id { get; } = CIEnvironment.Long("GITHUB_REPOSITORY_ID");
    /// <summary>Gets or configures the provider value for Owner.</summary>
    public string? Owner { get; } = CIEnvironment.String("GITHUB_REPOSITORY_OWNER");
    /// <summary>Gets or configures the provider value for OwnerId.</summary>
    public long? OwnerId { get; } = CIEnvironment.Long("GITHUB_REPOSITORY_OWNER_ID");
    /// <summary>Gets or configures the provider value for ServerUrl.</summary>
    public Uri? ServerUrl { get; } = CIEnvironment.Uri("GITHUB_SERVER_URL");
    /// <summary>Gets or configures the provider value for ApiUrl.</summary>
    public Uri? ApiUrl { get; } = CIEnvironment.Uri("GITHUB_API_URL");
    /// <summary>Gets or configures the provider value for GraphQlUrl.</summary>
    public Uri? GraphQlUrl { get; } = CIEnvironment.Uri("GITHUB_GRAPHQL_URL");
}

/// <summary>Represents provider data for GitHubRunMetadata.</summary>
public sealed record GitHubRunMetadata
{
    internal GitHubRunMetadata() { }
    /// <summary>Gets or configures the provider value for Id.</summary>
    public long? Id { get; } = CIEnvironment.Long("GITHUB_RUN_ID");
    /// <summary>Gets or configures the provider value for Number.</summary>
    public long? Number { get; } = CIEnvironment.Long("GITHUB_RUN_NUMBER");
    /// <summary>Gets or configures the provider value for Attempt.</summary>
    public long? Attempt { get; } = CIEnvironment.Long("GITHUB_RUN_ATTEMPT");
    /// <summary>Gets or configures the provider value for Job.</summary>
    public string? Job { get; } = CIEnvironment.String("GITHUB_JOB");
    /// <summary>Gets or configures the provider value for RetentionDays.</summary>
    public long? RetentionDays { get; } = CIEnvironment.Long("GITHUB_RETENTION_DAYS");
}

/// <summary>Represents provider data for GitHubCommandFileMetadata.</summary>
public sealed record GitHubCommandFileMetadata
{
    internal GitHubCommandFileMetadata() { }
    /// <summary>Gets or configures the provider value for Environment.</summary>
    public AbsolutePath? Environment { get; } = CIEnvironment.Path("GITHUB_ENV");
    /// <summary>Gets or configures the provider value for Output.</summary>
    public AbsolutePath? Output { get; } = CIEnvironment.Path("GITHUB_OUTPUT");
    /// <summary>Gets or configures the provider value for Path.</summary>
    public AbsolutePath? Path { get; } = CIEnvironment.Path("GITHUB_PATH");
    /// <summary>Gets or configures the provider value for State.</summary>
    public AbsolutePath? State { get; } = CIEnvironment.Path("GITHUB_STATE");
    /// <summary>Gets or configures the provider value for StepSummary.</summary>
    public AbsolutePath? StepSummary { get; } = CIEnvironment.Path("GITHUB_STEP_SUMMARY");
}

/// <summary>Represents provider data for GitHubRunnerMetadata.</summary>
public sealed record GitHubRunnerMetadata
{
    internal GitHubRunnerMetadata() { }
    /// <summary>Gets or configures the provider value for Name.</summary>
    public string? Name { get; } = CIEnvironment.String("RUNNER_NAME");
    /// <summary>Gets or configures the provider value for Os.</summary>
    public string? Os { get; } = CIEnvironment.String("RUNNER_OS");
    /// <summary>Gets or configures the provider value for Architecture.</summary>
    public string? Architecture { get; } = CIEnvironment.String("RUNNER_ARCH");
    /// <summary>Gets or configures the provider value for Environment.</summary>
    public string? Environment { get; } = CIEnvironment.String("RUNNER_ENVIRONMENT");
    /// <summary>Gets or configures the provider value for Debug.</summary>
    public bool? Debug { get; } = CIEnvironment.Bool("RUNNER_DEBUG");
    /// <summary>Gets or configures the provider value for TrackingId.</summary>
    public string? TrackingId { get; } = CIEnvironment.String("RUNNER_TRACKING_ID");
    /// <summary>Gets or configures the provider value for TempDirectory.</summary>
    public AbsolutePath? TempDirectory { get; } = CIEnvironment.Path("RUNNER_TEMP");
    /// <summary>Gets or configures the provider value for ToolCacheDirectory.</summary>
    public AbsolutePath? ToolCacheDirectory { get; } = CIEnvironment.Path("RUNNER_TOOL_CACHE");
    /// <summary>Gets or configures the provider value for Workspace.</summary>
    public AbsolutePath? Workspace { get; } = CIEnvironment.Path("GITHUB_WORKSPACE");
}

/// <summary>Represents provider data for GitHubWorkflowMetadata.</summary>
public sealed record GitHubWorkflowMetadata
{
    internal GitHubWorkflowMetadata() { }
    /// <summary>Gets or configures the provider value for Name.</summary>
    public string? Name { get; } = CIEnvironment.String("GITHUB_WORKFLOW");
    /// <summary>Gets or configures the provider value for WorkflowReference.</summary>
    public string? WorkflowReference { get; } = CIEnvironment.String("GITHUB_WORKFLOW_REF");
    /// <summary>Gets or configures the provider value for WorkflowSha.</summary>
    public string? WorkflowSha { get; } = CIEnvironment.String("GITHUB_WORKFLOW_SHA");
    /// <summary>Gets or configures the provider value for CommitSha.</summary>
    public string? CommitSha { get; } = CIEnvironment.String("GITHUB_SHA");
    /// <summary>Gets or configures the provider value for ReferenceName.</summary>
    public string? ReferenceName { get; } = CIEnvironment.String("GITHUB_REF_NAME");
    /// <summary>Gets or configures the provider value for ReferenceType.</summary>
    public string? ReferenceType { get; } = CIEnvironment.String("GITHUB_REF_TYPE");
    /// <summary>Gets or configures the provider value for GitReference.</summary>
    public string? GitReference { get; } = CIEnvironment.String("GITHUB_REF");
    /// <summary>Gets or configures the provider value for ReferenceProtected.</summary>
    public bool? ReferenceProtected { get; } = CIEnvironment.Bool("GITHUB_REF_PROTECTED");
    /// <summary>Gets or configures the provider value for HeadReference.</summary>
    public string? HeadReference { get; } = CIEnvironment.String("GITHUB_HEAD_REF");
    /// <summary>Gets or configures the provider value for BaseReference.</summary>
    public string? BaseReference { get; } = CIEnvironment.String("GITHUB_BASE_REF");
}
