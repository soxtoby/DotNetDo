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

    /// <summary>Gets or configures the provider value for Agent.</summary>
    public AzureAgentMetadata Agent { get; }
    /// <summary>Gets or configures the provider value for Build.</summary>
    public AzureBuildMetadata Build { get; }
    /// <summary>Gets or configures the provider value for Deployment.</summary>
    public AzureDeploymentMetadata Deployment { get; }
    /// <summary>Gets or configures the provider value for Pipeline.</summary>
    public AzurePipelineMetadata Pipeline { get; }
    /// <summary>Gets or configures the provider value for Release.</summary>
    public AzureReleaseMetadata Release { get; }
    /// <summary>Gets or configures the provider value for System.</summary>
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
        ("id", Required(id)), ("field", field switch
        {
            AzureEndpointField.AuthParameter => "authParameter",
            AzureEndpointField.DataParameter => "dataParameter",
            AzureEndpointField.Url => "url",
            _ => throw new ArgumentOutOfRangeException(nameof(field))
        }), ("key", field == AzureEndpointField.Url ? key : Required(key!)));

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

/// <summary>Represents provider data for AzureIssueType.</summary>
public enum AzureIssueType
{
    /// <summary>Records an error.</summary>
    Error,
    /// <summary>Records a warning.</summary>
    Warning
}

/// <summary>Represents provider data for AzureTaskResult.</summary>
public enum AzureTaskResult
{
    /// <summary>Completes successfully.</summary>
    Succeeded,
    /// <summary>Completes successfully with reported issues.</summary>
    SucceededWithIssues,
    /// <summary>Completes unsuccessfully.</summary>
    Failed
}

/// <summary>Represents provider data for AzureTimelineState.</summary>
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

/// <summary>Represents provider data for AzureEndpointField.</summary>
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

/// <summary>Represents provider data for AzureLogIssue.</summary>
public sealed record AzureLogIssue
{
    /// <summary>Gets or configures the provider value for Type.</summary>
    public required AzureIssueType Type { get; init; }
    /// <summary>Gets or configures the provider value for SourcePath.</summary>
    public RelativePath? SourcePath { get; init; }
    /// <summary>Gets or configures the provider value for LineNumber.</summary>
    public long? LineNumber { get; init; }
    /// <summary>Gets or configures the provider value for ColumnNumber.</summary>
    public long? ColumnNumber { get; init; }
    /// <summary>Gets or configures the provider value for Code.</summary>
    public string? Code { get; init; }
}

/// <summary>Represents provider data for AzureVariableOptions.</summary>
public sealed record AzureVariableOptions
{
    /// <summary>Gets or configures the provider value for IsSecret.</summary>
    public bool? IsSecret { get; init; }
    /// <summary>Gets or configures the provider value for IsOutput.</summary>
    public bool? IsOutput { get; init; }
    /// <summary>Gets or configures the provider value for IsReadOnly.</summary>
    public bool? IsReadOnly { get; init; }
}

/// <summary>Represents provider data for AzureLogDetail.</summary>
public sealed record AzureLogDetail
{
    /// <summary>Gets or configures the provider value for Id.</summary>
    public required Guid Id { get; init; }
    /// <summary>Gets or configures the provider value for ParentId.</summary>
    public Guid? ParentId { get; init; }
    /// <summary>Gets or configures the provider value for Type.</summary>
    public string? Type { get; init; }
    /// <summary>Gets or configures the provider value for Name.</summary>
    public string? Name { get; init; }
    /// <summary>Gets or configures the provider value for Order.</summary>
    public int? Order { get; init; }
    /// <summary>Gets or configures the provider value for StartTime.</summary>
    public DateTimeOffset? StartTime { get; init; }
    /// <summary>Gets or configures the provider value for FinishTime.</summary>
    public DateTimeOffset? FinishTime { get; init; }
    /// <summary>Gets or configures the provider value for Progress.</summary>
    public int? Progress { get; init; }
    /// <summary>Gets or configures the provider value for State.</summary>
    public AzureTimelineState? State { get; init; }
    /// <summary>Gets or configures the provider value for Result.</summary>
    public AzureTaskResult? Result { get; init; }
}

/// <summary>Represents provider data for AzureAgentMetadata.</summary>
public sealed record AzureAgentMetadata
{
    internal AzureAgentMetadata() { }
    /// <summary>Gets or configures the provider value for Id.</summary>
    public long? Id { get; } = CIEnvironment.Long("AGENT_ID");
    /// <summary>Gets or configures the provider value for Name.</summary>
    public string? Name { get; } = CIEnvironment.String("AGENT_NAME");
    /// <summary>Gets or configures the provider value for MachineName.</summary>
    public string? MachineName { get; } = CIEnvironment.String("AGENT_MACHINENAME");
    /// <summary>Gets or configures the provider value for Os.</summary>
    public string? Os { get; } = CIEnvironment.String("AGENT_OS");
    /// <summary>Gets or configures the provider value for Architecture.</summary>
    public string? Architecture { get; } = CIEnvironment.String("AGENT_OSARCHITECTURE");
    /// <summary>Gets or configures the provider value for Version.</summary>
    public string? Version { get; } = CIEnvironment.String("AGENT_VERSION");
    /// <summary>Gets or configures the provider value for BuildDirectory.</summary>
    public AbsolutePath? BuildDirectory { get; } = CIEnvironment.Path("AGENT_BUILDDIRECTORY");
    /// <summary>Gets or configures the provider value for HomeDirectory.</summary>
    public AbsolutePath? HomeDirectory { get; } = CIEnvironment.Path("AGENT_HOMEDIRECTORY");
    /// <summary>Gets or configures the provider value for TempDirectory.</summary>
    public AbsolutePath? TempDirectory { get; } = CIEnvironment.Path("AGENT_TEMPDIRECTORY");
    /// <summary>Gets or configures the provider value for ToolsDirectory.</summary>
    public AbsolutePath? ToolsDirectory { get; } = CIEnvironment.Path("AGENT_TOOLSDIRECTORY");
    /// <summary>Gets or configures the provider value for WorkFolder.</summary>
    public AbsolutePath? WorkFolder { get; } = CIEnvironment.Path("AGENT_WORKFOLDER");
    /// <summary>Gets or configures the provider value for JobName.</summary>
    public string? JobName { get; } = CIEnvironment.String("AGENT_JOBNAME");
    /// <summary>Gets or configures the provider value for JobStatus.</summary>
    public string? JobStatus { get; } = CIEnvironment.String("AGENT_JOBSTATUS");
}

/// <summary>Represents provider data for AzureBuildMetadata.</summary>
public sealed record AzureBuildMetadata
{
    internal AzureBuildMetadata() { }
    /// <summary>Gets or configures the provider value for Id.</summary>
    public long? Id { get; } = CIEnvironment.Long("BUILD_BUILDID");
    /// <summary>Gets or configures the provider value for Number.</summary>
    public string? Number { get; } = CIEnvironment.String("BUILD_BUILDNUMBER");
    /// <summary>Gets or configures the provider value for Uri.</summary>
    public Uri? Uri { get; } = CIEnvironment.Uri("BUILD_BUILDURI");
    /// <summary>Gets or configures the provider value for DefinitionName.</summary>
    public string? DefinitionName { get; } = CIEnvironment.String("BUILD_DEFINITIONNAME");
    /// <summary>Gets or configures the provider value for DefinitionId.</summary>
    public long? DefinitionId { get; } = CIEnvironment.Long("SYSTEM_DEFINITIONID");
    /// <summary>Gets or configures the provider value for Reason.</summary>
    public string? Reason { get; } = CIEnvironment.String("BUILD_REASON");
    /// <summary>Gets or configures the provider value for RepositoryName.</summary>
    public string? RepositoryName { get; } = CIEnvironment.String("BUILD_REPOSITORY_NAME");
    /// <summary>Gets or configures the provider value for RepositoryId.</summary>
    public string? RepositoryId { get; } = CIEnvironment.String("BUILD_REPOSITORY_ID");
    /// <summary>Gets or configures the provider value for RepositoryProvider.</summary>
    public string? RepositoryProvider { get; } = CIEnvironment.String("BUILD_REPOSITORY_PROVIDER");
    /// <summary>Gets or configures the provider value for RepositoryUri.</summary>
    public Uri? RepositoryUri { get; } = CIEnvironment.Uri("BUILD_REPOSITORY_URI");
    /// <summary>Gets or configures the provider value for RepositoryClean.</summary>
    public bool? RepositoryClean { get; } = CIEnvironment.Bool("BUILD_REPOSITORY_CLEAN");
    /// <summary>Gets or configures the provider value for RepositoryGitSubmoduleCheckout.</summary>
    public bool? RepositoryGitSubmoduleCheckout { get; } = CIEnvironment.Bool("BUILD_REPOSITORY_GIT_SUBMODULECHECKOUT");
    /// <summary>Gets or configures the provider value for SourceBranch.</summary>
    public string? SourceBranch { get; } = CIEnvironment.String("BUILD_SOURCEBRANCH");
    /// <summary>Gets or configures the provider value for SourceBranchName.</summary>
    public string? SourceBranchName { get; } = CIEnvironment.String("BUILD_SOURCEBRANCHNAME");
    /// <summary>Gets or configures the provider value for SourceVersion.</summary>
    public string? SourceVersion { get; } = CIEnvironment.String("BUILD_SOURCEVERSION");
    /// <summary>Gets or configures the provider value for SourceVersionMessage.</summary>
    public string? SourceVersionMessage { get; } = CIEnvironment.String("BUILD_SOURCEVERSIONMESSAGE");
    /// <summary>Gets or configures the provider value for SourcesDirectory.</summary>
    public AbsolutePath? SourcesDirectory { get; } = CIEnvironment.Path("BUILD_SOURCESDIRECTORY");
    /// <summary>Gets or configures the provider value for ArtifactStagingDirectory.</summary>
    public AbsolutePath? ArtifactStagingDirectory { get; } = CIEnvironment.Path("BUILD_ARTIFACTSTAGINGDIRECTORY");
    /// <summary>Gets or configures the provider value for BinariesDirectory.</summary>
    public AbsolutePath? BinariesDirectory { get; } = CIEnvironment.Path("BUILD_BINARIESDIRECTORY");
    /// <summary>Gets or configures the provider value for StagingDirectory.</summary>
    public AbsolutePath? StagingDirectory { get; } = CIEnvironment.Path("BUILD_STAGINGDIRECTORY");
    /// <summary>Gets or configures the provider value for RequestedFor.</summary>
    public string? RequestedFor { get; } = CIEnvironment.String("BUILD_REQUESTEDFOR");
    /// <summary>Gets or configures the provider value for RequestedForEmail.</summary>
    public string? RequestedForEmail { get; } = CIEnvironment.String("BUILD_REQUESTEDFOREMAIL");
    /// <summary>Gets or configures the provider value for RequestedForId.</summary>
    public string? RequestedForId { get; } = CIEnvironment.String("BUILD_REQUESTEDFORID");
}

/// <summary>Represents provider data for AzurePipelineMetadata.</summary>
public sealed record AzurePipelineMetadata
{
    internal AzurePipelineMetadata() { }
    /// <summary>Gets or configures the provider value for Workspace.</summary>
    public AbsolutePath? Workspace { get; } = CIEnvironment.Path("PIPELINE_WORKSPACE");
}

/// <summary>Represents provider data for AzureSystemMetadata.</summary>
public sealed record AzureSystemMetadata
{
    internal AzureSystemMetadata() { }
    /// <summary>Gets or configures the provider value for CollectionId.</summary>
    public Guid? CollectionId { get; } = CIEnvironment.Guid("SYSTEM_COLLECTIONID");
    /// <summary>Gets or configures the provider value for CollectionUri.</summary>
    public Uri? CollectionUri { get; } = CIEnvironment.Uri("SYSTEM_COLLECTIONURI");
    /// <summary>Gets or configures the provider value for TeamProject.</summary>
    public string? TeamProject { get; } = CIEnvironment.String("SYSTEM_TEAMPROJECT");
    /// <summary>Gets or configures the provider value for TeamProjectId.</summary>
    public string? TeamProjectId { get; } = CIEnvironment.String("SYSTEM_TEAMPROJECTID");
    /// <summary>Gets or configures the provider value for JobId.</summary>
    public string? JobId { get; } = CIEnvironment.String("SYSTEM_JOBID");
    /// <summary>Gets or configures the provider value for JobName.</summary>
    public string? JobName { get; } = CIEnvironment.String("SYSTEM_JOBNAME");
    /// <summary>Gets or configures the provider value for JobDisplayName.</summary>
    public string? JobDisplayName { get; } = CIEnvironment.String("SYSTEM_JOBDISPLAYNAME");
    /// <summary>Gets or configures the provider value for JobAttempt.</summary>
    public int? JobAttempt { get; } = (int?)CIEnvironment.Long("SYSTEM_JOBATTEMPT");
    /// <summary>Gets or configures the provider value for PhaseName.</summary>
    public string? PhaseName { get; } = CIEnvironment.String("SYSTEM_PHASENAME");
    /// <summary>Gets or configures the provider value for StageName.</summary>
    public string? StageName { get; } = CIEnvironment.String("SYSTEM_STAGENAME");
    /// <summary>Gets or configures the provider value for StageDisplayName.</summary>
    public string? StageDisplayName { get; } = CIEnvironment.String("SYSTEM_STAGEDISPLAYNAME");
    /// <summary>Gets or configures the provider value for StageAttempt.</summary>
    public int? StageAttempt { get; } = (int?)CIEnvironment.Long("SYSTEM_STAGEATTEMPT");
    /// <summary>Gets or configures the provider value for DefaultWorkingDirectory.</summary>
    public AbsolutePath? DefaultWorkingDirectory { get; } = CIEnvironment.Path("SYSTEM_DEFAULTWORKINGDIRECTORY");
    /// <summary>Gets or configures the provider value for TaskInstanceId.</summary>
    public string? TaskInstanceId { get; } = CIEnvironment.String("SYSTEM_TASKINSTANCEID");
}

/// <summary>Represents provider data for AzureDeploymentMetadata.</summary>
public sealed record AzureDeploymentMetadata
{
    internal AzureDeploymentMetadata() { }
    /// <summary>Gets or configures the provider value for EnvironmentName.</summary>
    public string? EnvironmentName { get; } = CIEnvironment.String("ENVIRONMENT_NAME");
    /// <summary>Gets or configures the provider value for EnvironmentId.</summary>
    public string? EnvironmentId { get; } = CIEnvironment.String("ENVIRONMENT_ID");
    /// <summary>Gets or configures the provider value for ResourceName.</summary>
    public string? ResourceName { get; } = CIEnvironment.String("ENVIRONMENT_RESOURCENAME");
    /// <summary>Gets or configures the provider value for ResourceId.</summary>
    public string? ResourceId { get; } = CIEnvironment.String("ENVIRONMENT_RESOURCEID");
    /// <summary>Gets or configures the provider value for StrategyName.</summary>
    public string? StrategyName { get; } = CIEnvironment.String("STRATEGY_NAME");
    /// <summary>Gets or configures the provider value for CycleName.</summary>
    public string? CycleName { get; } = CIEnvironment.String("STRATEGY_CYCLENAME");
}

/// <summary>Represents provider data for AzureReleaseMetadata.</summary>
public sealed record AzureReleaseMetadata
{
    internal AzureReleaseMetadata() { }
    /// <summary>Gets or configures the provider value for Id.</summary>
    public long? Id { get; } = CIEnvironment.Long("RELEASE_RELEASEID");
    /// <summary>Gets or configures the provider value for Name.</summary>
    public string? Name { get; } = CIEnvironment.String("RELEASE_RELEASENAME");
    /// <summary>Gets or configures the provider value for Description.</summary>
    public string? Description { get; } = CIEnvironment.String("RELEASE_RELEASEDESCRIPTION");
    /// <summary>Gets or configures the provider value for WebUrl.</summary>
    public Uri? WebUrl { get; } = CIEnvironment.Uri("RELEASE_RELEASEWEBURL");
    /// <summary>Gets or configures the provider value for EnvironmentName.</summary>
    public string? EnvironmentName { get; } = CIEnvironment.String("RELEASE_ENVIRONMENTNAME");
    /// <summary>Gets or configures the provider value for EnvironmentId.</summary>
    public long? EnvironmentId { get; } = CIEnvironment.Long("RELEASE_ENVIRONMENTID");
    /// <summary>Gets or configures the provider value for DefinitionName.</summary>
    public string? DefinitionName { get; } = CIEnvironment.String("RELEASE_DEFINITIONNAME");
    /// <summary>Gets or configures the provider value for DefinitionId.</summary>
    public long? DefinitionId { get; } = CIEnvironment.Long("RELEASE_DEFINITIONID");
    /// <summary>Gets or configures the provider value for DeploymentId.</summary>
    public string? DeploymentId { get; } = CIEnvironment.String("RELEASE_DEPLOYMENTID");
    /// <summary>Gets or configures the provider value for AttemptNumber.</summary>
    public int? AttemptNumber { get; } = (int?)CIEnvironment.Long("RELEASE_ATTEMPTNUMBER");
    /// <summary>Gets or configures the provider value for Reason.</summary>
    public string? Reason { get; } = CIEnvironment.String("RELEASE_REASON");
    /// <summary>Gets or configures the provider value for RequestedFor.</summary>
    public string? RequestedFor { get; } = CIEnvironment.String("RELEASE_REQUESTEDFOR");
    /// <summary>Gets or configures the provider value for RequestedForId.</summary>
    public string? RequestedForId { get; } = CIEnvironment.String("RELEASE_REQUESTEDFORID");
}
