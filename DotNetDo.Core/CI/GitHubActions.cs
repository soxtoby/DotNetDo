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

    /// <summary>The Action metadata read from the provider environment.</summary>
    public GitHubActionMetadata Action { get; }
    /// <summary>The Event metadata read from the provider environment.</summary>
    public GitHubEventMetadata Event { get; }
    /// <summary>The Files metadata read from the provider environment.</summary>
    public GitHubCommandFileMetadata Files { get; }
    /// <summary>The Repository metadata read from the provider environment.</summary>
    public GitHubRepositoryMetadata Repository { get; }
    /// <summary>The Run metadata read from the provider environment.</summary>
    public GitHubRunMetadata Run { get; }
    /// <summary>The Runner metadata read from the provider environment.</summary>
    public GitHubRunnerMetadata Runner { get; }
    /// <summary>The Workflow metadata read from the provider environment.</summary>
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

/// <summary>Optional source location attached to a GitHub Actions notice, warning, or error annotation.</summary>
public sealed record GitHubAnnotation
{
    /// <summary>The annotation title shown separately from its message.</summary>
    public string? Title { get; init; }
    /// <summary>The workspace-relative file associated with the annotation.</summary>
    public RelativePath? File { get; init; }
    /// <summary>The one-based starting line; required when a column is supplied.</summary>
    public long? Line { get; init; }
    /// <summary>The one-based ending line; omitted for a single-line annotation.</summary>
    public long? EndLine { get; init; }
    /// <summary>The one-based starting column; valid only for a single-line or same-line range.</summary>
    public long? Column { get; init; }
    /// <summary>The one-based ending column; valid only when the start and end lines are equal.</summary>
    public long? EndColumn { get; init; }
}

/// <summary>Environment metadata used by GitHubActionMetadata.</summary>
public sealed record GitHubActionMetadata
{
    internal GitHubActionMetadata() { }
    /// <summary>Reads <c>GITHUB_ACTION</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Name { get; } = CIEnvironment.String("GITHUB_ACTION");
    /// <summary>Reads <c>GITHUB_ACTION_REPOSITORY</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Repository { get; } = CIEnvironment.String("GITHUB_ACTION_REPOSITORY");
    /// <summary>Reads <c>GITHUB_ACTION_REF</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Reference { get; } = CIEnvironment.String("GITHUB_ACTION_REF");
    /// <summary>Reads <c>GITHUB_ACTION_PATH</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? Path { get; } = CIEnvironment.Path("GITHUB_ACTION_PATH");
}

/// <summary>Environment metadata used by GitHubEventMetadata.</summary>
public sealed record GitHubEventMetadata
{
    internal GitHubEventMetadata() { }
    /// <summary>Reads <c>GITHUB_EVENT_NAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Name { get; } = CIEnvironment.String("GITHUB_EVENT_NAME");
    /// <summary>Reads <c>GITHUB_EVENT_PATH</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? Path { get; } = CIEnvironment.Path("GITHUB_EVENT_PATH");
    /// <summary>Reads <c>GITHUB_ACTOR</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Actor { get; } = CIEnvironment.String("GITHUB_ACTOR");
    /// <summary>Reads <c>GITHUB_ACTOR_ID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public long? ActorId { get; } = CIEnvironment.Long("GITHUB_ACTOR_ID");
    /// <summary>Reads <c>GITHUB_TRIGGERING_ACTOR</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? TriggeringActor { get; } = CIEnvironment.String("GITHUB_TRIGGERING_ACTOR");
}

/// <summary>Environment metadata used by GitHubRepositoryMetadata.</summary>
public sealed record GitHubRepositoryMetadata
{
    internal GitHubRepositoryMetadata() { }
    /// <summary>Reads <c>GITHUB_REPOSITORY</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Name { get; } = CIEnvironment.String("GITHUB_REPOSITORY");
    /// <summary>Reads <c>GITHUB_REPOSITORY_ID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public long? Id { get; } = CIEnvironment.Long("GITHUB_REPOSITORY_ID");
    /// <summary>Reads <c>GITHUB_REPOSITORY_OWNER</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Owner { get; } = CIEnvironment.String("GITHUB_REPOSITORY_OWNER");
    /// <summary>Reads <c>GITHUB_REPOSITORY_OWNER_ID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public long? OwnerId { get; } = CIEnvironment.Long("GITHUB_REPOSITORY_OWNER_ID");
    /// <summary>Reads <c>GITHUB_SERVER_URL</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public Uri? ServerUrl { get; } = CIEnvironment.Uri("GITHUB_SERVER_URL");
    /// <summary>Reads <c>GITHUB_API_URL</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public Uri? ApiUrl { get; } = CIEnvironment.Uri("GITHUB_API_URL");
    /// <summary>Reads <c>GITHUB_GRAPHQL_URL</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public Uri? GraphQlUrl { get; } = CIEnvironment.Uri("GITHUB_GRAPHQL_URL");
}

/// <summary>Environment metadata used by GitHubRunMetadata.</summary>
public sealed record GitHubRunMetadata
{
    internal GitHubRunMetadata() { }
    /// <summary>Reads <c>GITHUB_RUN_ID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public long? Id { get; } = CIEnvironment.Long("GITHUB_RUN_ID");
    /// <summary>Reads <c>GITHUB_RUN_NUMBER</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public long? Number { get; } = CIEnvironment.Long("GITHUB_RUN_NUMBER");
    /// <summary>Reads <c>GITHUB_RUN_ATTEMPT</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public long? Attempt { get; } = CIEnvironment.Long("GITHUB_RUN_ATTEMPT");
    /// <summary>Reads <c>GITHUB_JOB</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Job { get; } = CIEnvironment.String("GITHUB_JOB");
    /// <summary>Reads <c>GITHUB_RETENTION_DAYS</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public long? RetentionDays { get; } = CIEnvironment.Long("GITHUB_RETENTION_DAYS");
}

/// <summary>Environment metadata used by GitHubCommandFileMetadata.</summary>
public sealed record GitHubCommandFileMetadata
{
    internal GitHubCommandFileMetadata() { }
    /// <summary>Reads <c>GITHUB_ENV</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? Environment { get; } = CIEnvironment.Path("GITHUB_ENV");
    /// <summary>Reads <c>GITHUB_OUTPUT</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? Output { get; } = CIEnvironment.Path("GITHUB_OUTPUT");
    /// <summary>Reads <c>GITHUB_PATH</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? Path { get; } = CIEnvironment.Path("GITHUB_PATH");
    /// <summary>Reads <c>GITHUB_STATE</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? State { get; } = CIEnvironment.Path("GITHUB_STATE");
    /// <summary>Reads <c>GITHUB_STEP_SUMMARY</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? StepSummary { get; } = CIEnvironment.Path("GITHUB_STEP_SUMMARY");
}

/// <summary>Environment metadata used by GitHubRunnerMetadata.</summary>
public sealed record GitHubRunnerMetadata
{
    internal GitHubRunnerMetadata() { }
    /// <summary>Reads <c>RUNNER_NAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Name { get; } = CIEnvironment.String("RUNNER_NAME");
    /// <summary>Reads <c>RUNNER_OS</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Os { get; } = CIEnvironment.String("RUNNER_OS");
    /// <summary>Reads <c>RUNNER_ARCH</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Architecture { get; } = CIEnvironment.String("RUNNER_ARCH");
    /// <summary>Reads <c>RUNNER_ENVIRONMENT</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Environment { get; } = CIEnvironment.String("RUNNER_ENVIRONMENT");
    /// <summary>Reads <c>RUNNER_DEBUG</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public bool? Debug { get; } = CIEnvironment.Bool("RUNNER_DEBUG");
    /// <summary>Reads <c>RUNNER_TRACKING_ID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? TrackingId { get; } = CIEnvironment.String("RUNNER_TRACKING_ID");
    /// <summary>Reads <c>RUNNER_TEMP</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? TempDirectory { get; } = CIEnvironment.Path("RUNNER_TEMP");
    /// <summary>Reads <c>RUNNER_TOOL_CACHE</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? ToolCacheDirectory { get; } = CIEnvironment.Path("RUNNER_TOOL_CACHE");
    /// <summary>Reads <c>GITHUB_WORKSPACE</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? Workspace { get; } = CIEnvironment.Path("GITHUB_WORKSPACE");
}

/// <summary>Environment metadata used by GitHubWorkflowMetadata.</summary>
public sealed record GitHubWorkflowMetadata
{
    internal GitHubWorkflowMetadata() { }
    /// <summary>Reads <c>GITHUB_WORKFLOW</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Name { get; } = CIEnvironment.String("GITHUB_WORKFLOW");
    /// <summary>Reads <c>GITHUB_WORKFLOW_REF</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? WorkflowReference { get; } = CIEnvironment.String("GITHUB_WORKFLOW_REF");
    /// <summary>Reads <c>GITHUB_WORKFLOW_SHA</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? WorkflowSha { get; } = CIEnvironment.String("GITHUB_WORKFLOW_SHA");
    /// <summary>Reads <c>GITHUB_SHA</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? CommitSha { get; } = CIEnvironment.String("GITHUB_SHA");
    /// <summary>Reads <c>GITHUB_REF_NAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? ReferenceName { get; } = CIEnvironment.String("GITHUB_REF_NAME");
    /// <summary>Reads <c>GITHUB_REF_TYPE</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? ReferenceType { get; } = CIEnvironment.String("GITHUB_REF_TYPE");
    /// <summary>Reads <c>GITHUB_REF</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? GitReference { get; } = CIEnvironment.String("GITHUB_REF");
    /// <summary>Reads <c>GITHUB_REF_PROTECTED</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public bool? ReferenceProtected { get; } = CIEnvironment.Bool("GITHUB_REF_PROTECTED");
    /// <summary>Reads <c>GITHUB_HEAD_REF</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? HeadReference { get; } = CIEnvironment.String("GITHUB_HEAD_REF");
    /// <summary>Reads <c>GITHUB_BASE_REF</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? BaseReference { get; } = CIEnvironment.String("GITHUB_BASE_REF");
}
