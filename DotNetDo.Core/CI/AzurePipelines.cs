namespace DotNetDo;

public static partial class Do
{
    static readonly Lazy<AzurePipelines?> AzurePipelinesInstance = new(() =>
        CIEnvironment.IsTrue("TF_BUILD") ? new AzurePipelines() : null);

    /// <summary>The active Azure Pipelines agent, or <see langword="null"/> outside Azure Pipelines.</summary>
    public static AzurePipelines? AzurePipelines => AzurePipelinesInstance.Value;
}

/// <summary>Exposes Azure Pipelines logging commands and predefined metadata.</summary>
public sealed class AzurePipelines
{
    readonly Lock _gate = new();

    internal AzurePipelines()
    {
        Agent = new();
        Build = new();
        Deployment = new();
        Pipeline = new();
        Release = new();
        System = new();
    }

    /// <summary>The Agent metadata read from the provider environment.</summary>
    public AzureAgentMetadata Agent { get; }
    /// <summary>The Build metadata read from the provider environment.</summary>
    public AzureBuildMetadata Build { get; }
    /// <summary>The Deployment metadata read from the provider environment.</summary>
    public AzureDeploymentMetadata Deployment { get; }
    /// <summary>The Pipeline metadata read from the provider environment.</summary>
    public AzurePipelineMetadata Pipeline { get; }
    /// <summary>The Release metadata read from the provider environment.</summary>
    public AzureReleaseMetadata Release { get; }
    /// <summary>The System metadata read from the provider environment.</summary>
    public AzureSystemMetadata System { get; }

    /// <summary>Emits the provider's Debug command immediately.</summary>
    public void Debug(string message) => Format("debug", message);
    /// <summary>Emits the provider's Command command immediately.</summary>
    public void Command(string message) => Format("command", message);
    /// <summary>Emits the provider's Warning command immediately.</summary>
    public void Warning(string message) => Format("warning", message);
    /// <summary>Emits the provider's Error command immediately.</summary>
    public void Error(string message) => Format("error", message);
    /// <summary>Emits the provider's Section command immediately.</summary>
    public void Section(string message) => Format("section", message);
    /// <summary>Emits the provider's StartGroup command immediately.</summary>
    public void StartGroup(string title) => Format("group", title);
    /// <summary>Emits the provider's EndGroup command immediately.</summary>
    public void EndGroup() => Write("##[endgroup]");

    /// <summary>Emits the provider's LogIssue command immediately.</summary>
    public void LogIssue(string message, AzureLogIssue options) => Vso("task.logissue", message,
        ("type", options.Type.ToString().ToLowerInvariant()), ("sourcepath", options.SourcePath?.UnixPath),
        ("linenumber", options.LineNumber), ("columnnumber", options.ColumnNumber), ("code", options.Code));
    /// <summary>Emits the provider's Complete command immediately.</summary>
    public void Complete(string? message = null, AzureTaskResult? result = null) =>
        Vso("task.complete", message ?? "", ("result", result));
    /// <summary>Emits the provider's SetProgress command immediately.</summary>
    public void SetProgress(int progress, string operation) =>
        Vso("task.setprogress", operation, ("value", progress));
    /// <summary>Emits the provider's LogDetail command immediately.</summary>
    public void LogDetail(string operation, AzureLogDetail options) => Vso("task.logdetail", operation,
        ("id", options.Id), ("parentid", options.ParentId), ("type", options.Type), ("name", options.Name),
        ("order", options.Order), ("starttime", options.StartTime), ("finishtime", options.FinishTime),
        ("progress", options.Progress), ("state", options.State), ("result", options.Result));
    /// <summary>Emits the provider's SetVariable command immediately.</summary>
    public void SetVariable(string name, string value, AzureVariableOptions? options = null) => Vso("task.setvariable", value,
        ("variable", Required(name)), ("issecret", options?.IsSecret), ("isoutput", options?.IsOutput),
        ("isreadonly", options?.IsReadOnly));
    /// <summary>Emits the provider's SetSecret command immediately.</summary>
    public void SetSecret(string value) => Vso("task.setsecret", value);
    /// <summary>Emits the provider's PrependPath command immediately.</summary>
    public void PrependPath(AbsolutePath path) => Vso("task.prependpath", path);
    /// <summary>Emits the provider's AddAttachment command immediately.</summary>
    public void AddAttachment(AbsolutePath path, string type, string name) => Vso("task.addattachment", path,
        ("type", Required(type)), ("name", Required(name)));
    /// <summary>Emits the provider's UploadFile command immediately.</summary>
    public void UploadFile(AbsolutePath path) => Vso("task.uploadfile", path);
    /// <summary>Emits the provider's UploadSummary command immediately.</summary>
    public void UploadSummary(AbsolutePath path) => Vso("task.uploadsummary", path);
    /// <summary>Emits the provider's SetEndpoint command immediately.</summary>
    public void SetEndpoint(string id, AzureEndpointField field, string value, string? key = null) => Vso("task.setendpoint", value,
        ("id", Required(id)), ("field", field.ToString().ToCamelCase()),
        ("key", field == AzureEndpointField.Url ? key : Required(key!)));
    /// <summary>Emits the provider's AssociateArtifact command immediately.</summary>
    public void AssociateArtifact(string artifactName, AzureArtifactType type, string location) => Vso("artifact.associate", location,
        ("artifactname", Required(artifactName)), ("type", type.ToString().ToLowerInvariant()));
    /// <summary>Associates an artifact using a provider-defined custom type.</summary>
    public void AssociateArtifact(string artifactName, string artifactType, Uri location) => Vso("artifact.associate", location,
        ("artifactname", Required(artifactName)), ("artifacttype", Required(artifactType)));
    /// <summary>Emits the provider's UploadArtifact command immediately.</summary>
    public void UploadArtifact(AbsolutePath path, string artifactName, string? containerFolder = null) => Vso("artifact.upload", path,
        ("artifactname", Required(artifactName)), ("containerfolder", containerFolder));
    /// <summary>Emits the provider's AddBuildTag command immediately.</summary>
    public void AddBuildTag(string tag) => Vso("build.addbuildtag", tag);
    /// <summary>Emits the provider's UpdateBuildNumber command immediately.</summary>
    public void UpdateBuildNumber(string number) => Vso("build.updatebuildnumber", number);
    /// <summary>Emits the provider's UploadLog command immediately.</summary>
    public void UploadLog(AbsolutePath path) => Vso("build.uploadlog", path);
    /// <summary>Emits the provider's UpdateReleaseName command immediately.</summary>
    public void UpdateReleaseName(string name) => Vso("release.updatereleasename", name);

    void Format(string command, string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Write($"##[{command}]{message}");
    }

    void Vso(string command, object message, params (string Name, object? Value)[] properties)
    {
        ArgumentNullException.ThrowIfNull(message);
        var rendered = string.Concat(properties.Where(property => property.Value is not null)
            .Select(property => $"{property.Name}={Escape(property.Value!)};"));
        Write($"##vso[{command}{(rendered.Length == 0 ? "" : " " + rendered)}]{Escape(message)}");
    }

    void Write(string value)
    {
        lock (_gate) Console.WriteLine(value);
    }

    static string Escape(object value) => Render(value)
        .Replace("%", "%AZP25", StringComparison.Ordinal)
        .Replace("\r", "%0D", StringComparison.Ordinal)
        .Replace("\n", "%0A", StringComparison.Ordinal)
        .Replace(";", "%3B", StringComparison.Ordinal)
        .Replace("]", "%5D", StringComparison.Ordinal);

    static string Render(object value) => value switch
    {
        bool boolean => boolean ? "true" : "false",
        DateTimeOffset timestamp => timestamp.ToString("O"),
        Enum @enum => @enum.ToString(),
        _ => value.ToString() ?? ""
    };

    static string Required(string value) { ArgumentException.ThrowIfNullOrWhiteSpace(value); return value; }
}

/// <summary>Environment metadata used by AzureIssueType.</summary>
public enum AzureIssueType
{
    /// <summary>Records an error.</summary>
    Error,
    /// <summary>Records a warning.</summary>
    Warning
}

/// <summary>Environment metadata used by AzureTaskResult.</summary>
public enum AzureTaskResult
{
    /// <summary>Completes successfully.</summary>
    Succeeded,
    /// <summary>Completes successfully with reported issues.</summary>
    SucceededWithIssues,
    /// <summary>Completes unsuccessfully.</summary>
    Failed
}

/// <summary>Environment metadata used by AzureTimelineState.</summary>
public enum AzureTimelineState
{
    /// <summary>The agent has no known state.</summary>
    Unknown,
    /// <summary>The record is initialized.</summary>
    Initialized,
    /// <summary>The record is in progress.</summary>
    InProgress,
    /// <summary>The record is complete.</summary>
    Completed
}

/// <summary>Environment metadata used by AzureEndpointField.</summary>
public enum AzureEndpointField
{
    /// <summary>Updates an endpoint authentication parameter.</summary>
    AuthParameter,
    /// <summary>Updates an endpoint data parameter.</summary>
    DataParameter,
    /// <summary>Updates the endpoint URL.</summary>
    Url
}

/// <summary>Identifies an Azure Pipelines artifact location type.</summary>
public enum AzureArtifactType
{
    /// <summary>An Azure Pipelines file container.</summary>
    Container,
    /// <summary>A shared filesystem path.</summary>
    FilePath,
    /// <summary>A version-control path.</summary>
    VersionControl,
    /// <summary>A Git reference.</summary>
    GitRef,
    /// <summary>A TFVC label.</summary>
    TfvcLabel
}

/// <summary>Fields attached to an Azure Pipelines error or warning timeline issue.</summary>
public sealed record AzureLogIssue
{
    /// <summary>Whether the issue is recorded as an error or warning.</summary>
    public required AzureIssueType Type { get; init; }
    /// <summary>The source file path associated with the issue.</summary>
    public RelativePath? SourcePath { get; init; }
    /// <summary>The one-based source line associated with the issue.</summary>
    public long? LineNumber { get; init; }
    /// <summary>The one-based source column associated with the issue.</summary>
    public long? ColumnNumber { get; init; }
    /// <summary>The diagnostic or error code associated with the issue.</summary>
    public string? Code { get; init; }
}

/// <summary>Controls how an Azure Pipelines task variable is stored and exposed.</summary>
public sealed record AzureVariableOptions
{
    /// <summary>Whether Azure Pipelines masks the value and withholds it from automatic environment mapping.</summary>
    public bool? IsSecret { get; init; }
    /// <summary>Whether later jobs and stages may consume the variable as a task output.</summary>
    public bool? IsOutput { get; init; }
    /// <summary>Whether later logging commands are prevented from changing the variable.</summary>
    public bool? IsReadOnly { get; init; }
}

/// <summary>Fields used to create or update an Azure Pipelines timeline record.</summary>
public sealed record AzureLogDetail
{
    /// <summary>The stable identifier used to update the same timeline record.</summary>
    public required Guid Id { get; init; }
    /// <summary>The parent timeline record identifier, when nesting records.</summary>
    public Guid? ParentId { get; init; }
    /// <summary>The caller-defined record category.</summary>
    public string? Type { get; init; }
    /// <summary>The display name shown for the record.</summary>
    public string? Name { get; init; }
    /// <summary>The record's display order among siblings.</summary>
    public int? Order { get; init; }
    /// <summary>The time work represented by the record started.</summary>
    public DateTimeOffset? StartTime { get; init; }
    /// <summary>The time work represented by the record finished.</summary>
    public DateTimeOffset? FinishTime { get; init; }
    /// <summary>The completion percentage reported for the record.</summary>
    public int? Progress { get; init; }
    /// <summary>The current timeline lifecycle state.</summary>
    public AzureTimelineState? State { get; init; }
    /// <summary>The completed task result, when known.</summary>
    public AzureTaskResult? Result { get; init; }
}

/// <summary>Environment metadata used by AzureAgentMetadata.</summary>
public sealed record AzureAgentMetadata
{
    internal AzureAgentMetadata() { }
    /// <summary>Reads <c>AGENT_ID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public long? Id { get; } = CIEnvironment.Long("AGENT_ID");
    /// <summary>Reads <c>AGENT_NAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Name { get; } = CIEnvironment.String("AGENT_NAME");
    /// <summary>Reads <c>AGENT_MACHINENAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? MachineName { get; } = CIEnvironment.String("AGENT_MACHINENAME");
    /// <summary>Reads <c>AGENT_OS</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Os { get; } = CIEnvironment.String("AGENT_OS");
    /// <summary>Reads <c>AGENT_OSARCHITECTURE</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Architecture { get; } = CIEnvironment.String("AGENT_OSARCHITECTURE");
    /// <summary>Reads <c>AGENT_VERSION</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Version { get; } = CIEnvironment.String("AGENT_VERSION");
    /// <summary>Reads <c>AGENT_BUILDDIRECTORY</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? BuildDirectory { get; } = CIEnvironment.Path("AGENT_BUILDDIRECTORY");
    /// <summary>Reads <c>AGENT_HOMEDIRECTORY</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? HomeDirectory { get; } = CIEnvironment.Path("AGENT_HOMEDIRECTORY");
    /// <summary>Reads <c>AGENT_TEMPDIRECTORY</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? TempDirectory { get; } = CIEnvironment.Path("AGENT_TEMPDIRECTORY");
    /// <summary>Reads <c>AGENT_TOOLSDIRECTORY</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? ToolsDirectory { get; } = CIEnvironment.Path("AGENT_TOOLSDIRECTORY");
    /// <summary>Reads <c>AGENT_WORKFOLDER</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? WorkFolder { get; } = CIEnvironment.Path("AGENT_WORKFOLDER");
    /// <summary>Reads <c>AGENT_JOBNAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? JobName { get; } = CIEnvironment.String("AGENT_JOBNAME");
    /// <summary>Reads <c>AGENT_JOBSTATUS</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? JobStatus { get; } = CIEnvironment.String("AGENT_JOBSTATUS");
}

/// <summary>Environment metadata used by AzureBuildMetadata.</summary>
public sealed record AzureBuildMetadata
{
    internal AzureBuildMetadata() { }
    /// <summary>Reads <c>BUILD_BUILDID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public long? Id { get; } = CIEnvironment.Long("BUILD_BUILDID");
    /// <summary>Reads <c>BUILD_BUILDNUMBER</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Number { get; } = CIEnvironment.String("BUILD_BUILDNUMBER");
    /// <summary>Reads <c>BUILD_BUILDURI</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public Uri? Uri { get; } = CIEnvironment.Uri("BUILD_BUILDURI");
    /// <summary>Reads <c>BUILD_DEFINITIONNAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? DefinitionName { get; } = CIEnvironment.String("BUILD_DEFINITIONNAME");
    /// <summary>Reads <c>SYSTEM_DEFINITIONID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public long? DefinitionId { get; } = CIEnvironment.Long("SYSTEM_DEFINITIONID");
    /// <summary>Reads <c>BUILD_REASON</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Reason { get; } = CIEnvironment.String("BUILD_REASON");
    /// <summary>Reads <c>BUILD_REPOSITORY_NAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? RepositoryName { get; } = CIEnvironment.String("BUILD_REPOSITORY_NAME");
    /// <summary>Reads <c>BUILD_REPOSITORY_ID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? RepositoryId { get; } = CIEnvironment.String("BUILD_REPOSITORY_ID");
    /// <summary>Reads <c>BUILD_REPOSITORY_PROVIDER</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? RepositoryProvider { get; } = CIEnvironment.String("BUILD_REPOSITORY_PROVIDER");
    /// <summary>Reads <c>BUILD_REPOSITORY_URI</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public Uri? RepositoryUri { get; } = CIEnvironment.Uri("BUILD_REPOSITORY_URI");
    /// <summary>Reads <c>BUILD_REPOSITORY_CLEAN</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public bool? RepositoryClean { get; } = CIEnvironment.Bool("BUILD_REPOSITORY_CLEAN");
    /// <summary>Reads <c>BUILD_REPOSITORY_GIT_SUBMODULECHECKOUT</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public bool? RepositoryGitSubmoduleCheckout { get; } = CIEnvironment.Bool("BUILD_REPOSITORY_GIT_SUBMODULECHECKOUT");
    /// <summary>Reads <c>BUILD_SOURCEBRANCH</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? SourceBranch { get; } = CIEnvironment.String("BUILD_SOURCEBRANCH");
    /// <summary>Reads <c>BUILD_SOURCEBRANCHNAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? SourceBranchName { get; } = CIEnvironment.String("BUILD_SOURCEBRANCHNAME");
    /// <summary>Reads <c>BUILD_SOURCEVERSION</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? SourceVersion { get; } = CIEnvironment.String("BUILD_SOURCEVERSION");
    /// <summary>Reads <c>BUILD_SOURCEVERSIONMESSAGE</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? SourceVersionMessage { get; } = CIEnvironment.String("BUILD_SOURCEVERSIONMESSAGE");
    /// <summary>Reads <c>BUILD_SOURCESDIRECTORY</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? SourcesDirectory { get; } = CIEnvironment.Path("BUILD_SOURCESDIRECTORY");
    /// <summary>Reads <c>BUILD_ARTIFACTSTAGINGDIRECTORY</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? ArtifactStagingDirectory { get; } = CIEnvironment.Path("BUILD_ARTIFACTSTAGINGDIRECTORY");
    /// <summary>Reads <c>BUILD_BINARIESDIRECTORY</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? BinariesDirectory { get; } = CIEnvironment.Path("BUILD_BINARIESDIRECTORY");
    /// <summary>Reads <c>BUILD_STAGINGDIRECTORY</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? StagingDirectory { get; } = CIEnvironment.Path("BUILD_STAGINGDIRECTORY");
    /// <summary>Reads <c>BUILD_REQUESTEDFOR</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? RequestedFor { get; } = CIEnvironment.String("BUILD_REQUESTEDFOR");
    /// <summary>Reads <c>BUILD_REQUESTEDFOREMAIL</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? RequestedForEmail { get; } = CIEnvironment.String("BUILD_REQUESTEDFOREMAIL");
    /// <summary>Reads <c>BUILD_REQUESTEDFORID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? RequestedForId { get; } = CIEnvironment.String("BUILD_REQUESTEDFORID");
}

/// <summary>Environment metadata used by AzurePipelineMetadata.</summary>
public sealed record AzurePipelineMetadata
{
    internal AzurePipelineMetadata() { }
    /// <summary>Reads <c>PIPELINE_WORKSPACE</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? Workspace { get; } = CIEnvironment.Path("PIPELINE_WORKSPACE");
}

/// <summary>Environment metadata used by AzureSystemMetadata.</summary>
public sealed record AzureSystemMetadata
{
    internal AzureSystemMetadata() { }
    /// <summary>Reads <c>SYSTEM_COLLECTIONID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public Guid? CollectionId { get; } = CIEnvironment.Guid("SYSTEM_COLLECTIONID");
    /// <summary>Reads <c>SYSTEM_COLLECTIONURI</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public Uri? CollectionUri { get; } = CIEnvironment.Uri("SYSTEM_COLLECTIONURI");
    /// <summary>Reads <c>SYSTEM_TEAMPROJECT</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? TeamProject { get; } = CIEnvironment.String("SYSTEM_TEAMPROJECT");
    /// <summary>Reads <c>SYSTEM_TEAMPROJECTID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? TeamProjectId { get; } = CIEnvironment.String("SYSTEM_TEAMPROJECTID");
    /// <summary>Reads <c>SYSTEM_JOBID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? JobId { get; } = CIEnvironment.String("SYSTEM_JOBID");
    /// <summary>Reads <c>SYSTEM_JOBNAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? JobName { get; } = CIEnvironment.String("SYSTEM_JOBNAME");
    /// <summary>Reads <c>SYSTEM_JOBDISPLAYNAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? JobDisplayName { get; } = CIEnvironment.String("SYSTEM_JOBDISPLAYNAME");
    /// <summary>The JobAttempt metadata read from the provider environment.</summary>
    public int? JobAttempt { get; } = (int?)CIEnvironment.Long("SYSTEM_JOBATTEMPT");
    /// <summary>Reads <c>SYSTEM_PHASENAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? PhaseName { get; } = CIEnvironment.String("SYSTEM_PHASENAME");
    /// <summary>Reads <c>SYSTEM_STAGENAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? StageName { get; } = CIEnvironment.String("SYSTEM_STAGENAME");
    /// <summary>Reads <c>SYSTEM_STAGEDISPLAYNAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? StageDisplayName { get; } = CIEnvironment.String("SYSTEM_STAGEDISPLAYNAME");
    /// <summary>The StageAttempt metadata read from the provider environment.</summary>
    public int? StageAttempt { get; } = (int?)CIEnvironment.Long("SYSTEM_STAGEATTEMPT");
    /// <summary>Reads <c>SYSTEM_DEFAULTWORKINGDIRECTORY</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public AbsolutePath? DefaultWorkingDirectory { get; } = CIEnvironment.Path("SYSTEM_DEFAULTWORKINGDIRECTORY");
    /// <summary>Reads <c>SYSTEM_TASKINSTANCEID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? TaskInstanceId { get; } = CIEnvironment.String("SYSTEM_TASKINSTANCEID");
}

/// <summary>Environment metadata used by AzureDeploymentMetadata.</summary>
public sealed record AzureDeploymentMetadata
{
    internal AzureDeploymentMetadata() { }
    /// <summary>Reads <c>ENVIRONMENT_NAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? EnvironmentName { get; } = CIEnvironment.String("ENVIRONMENT_NAME");
    /// <summary>Reads <c>ENVIRONMENT_ID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? EnvironmentId { get; } = CIEnvironment.String("ENVIRONMENT_ID");
    /// <summary>Reads <c>ENVIRONMENT_RESOURCENAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? ResourceName { get; } = CIEnvironment.String("ENVIRONMENT_RESOURCENAME");
    /// <summary>Reads <c>ENVIRONMENT_RESOURCEID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? ResourceId { get; } = CIEnvironment.String("ENVIRONMENT_RESOURCEID");
    /// <summary>Reads <c>STRATEGY_NAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? StrategyName { get; } = CIEnvironment.String("STRATEGY_NAME");
    /// <summary>Reads <c>STRATEGY_CYCLENAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? CycleName { get; } = CIEnvironment.String("STRATEGY_CYCLENAME");
}

/// <summary>Environment metadata used by AzureReleaseMetadata.</summary>
public sealed record AzureReleaseMetadata
{
    internal AzureReleaseMetadata() { }
    /// <summary>Reads <c>RELEASE_RELEASEID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public long? Id { get; } = CIEnvironment.Long("RELEASE_RELEASEID");
    /// <summary>Reads <c>RELEASE_RELEASENAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Name { get; } = CIEnvironment.String("RELEASE_RELEASENAME");
    /// <summary>Reads <c>RELEASE_RELEASEDESCRIPTION</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Description { get; } = CIEnvironment.String("RELEASE_RELEASEDESCRIPTION");
    /// <summary>Reads <c>RELEASE_RELEASEWEBURL</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public Uri? WebUrl { get; } = CIEnvironment.Uri("RELEASE_RELEASEWEBURL");
    /// <summary>Reads <c>RELEASE_ENVIRONMENTNAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? EnvironmentName { get; } = CIEnvironment.String("RELEASE_ENVIRONMENTNAME");
    /// <summary>Reads <c>RELEASE_ENVIRONMENTID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public long? EnvironmentId { get; } = CIEnvironment.Long("RELEASE_ENVIRONMENTID");
    /// <summary>Reads <c>RELEASE_DEFINITIONNAME</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? DefinitionName { get; } = CIEnvironment.String("RELEASE_DEFINITIONNAME");
    /// <summary>Reads <c>RELEASE_DEFINITIONID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public long? DefinitionId { get; } = CIEnvironment.Long("RELEASE_DEFINITIONID");
    /// <summary>Reads <c>RELEASE_DEPLOYMENTID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? DeploymentId { get; } = CIEnvironment.String("RELEASE_DEPLOYMENTID");
    /// <summary>The AttemptNumber metadata read from the provider environment.</summary>
    public int? AttemptNumber { get; } = (int?)CIEnvironment.Long("RELEASE_ATTEMPTNUMBER");
    /// <summary>Reads <c>RELEASE_REASON</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? Reason { get; } = CIEnvironment.String("RELEASE_REASON");
    /// <summary>Reads <c>RELEASE_REQUESTEDFOR</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? RequestedFor { get; } = CIEnvironment.String("RELEASE_REQUESTEDFOR");
    /// <summary>Reads <c>RELEASE_REQUESTEDFORID</c>; returns <see langword="null"/> when it is unset or empty.</summary>
    public string? RequestedForId { get; } = CIEnvironment.String("RELEASE_REQUESTEDFORID");
}
