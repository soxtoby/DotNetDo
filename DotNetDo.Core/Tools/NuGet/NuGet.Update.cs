namespace DotNetDo;
/// <summary>Updates packages in legacy NuGet project formats.</summary>
public sealed record NuGetUpdate : NuGetConfiguredCommand
{
    /// <summary>The solution or packages.config file whose dependencies are updated.</summary>
    public AbsolutePath? ConfigPath { get; init; }
    /// <summary>The rule used to select dependency versions.</summary>
    public NuGetDependencyVersion? DependencyVersion { get; init; }
    /// <summary>The action taken when an updated package conflicts with an existing file.</summary>
    public NuGetFileConflictAction? FileConflictAction { get; init; }
    /// <summary>Package IDs selected for update.</summary>
    public IReadOnlyList<string> Ids { get; init => field = Snapshot(value); } = [];
    /// <summary>The directory containing the MSBuild executable to use.</summary>
    public AbsolutePath? MSBuildPath { get; init; }
    /// <summary>The MSBuild version to use when locating MSBuild.</summary>
    public string? MSBuildVersion { get; init; }
    /// <summary>Includes prerelease packages.</summary>
    public bool Prerelease { get; init; }
    /// <summary>The directory containing installed packages.</summary>
    public AbsolutePath? RepositoryPath { get; init; }
    /// <summary>Restricts updates to versions with the same major and minor components.</summary>
    public bool Safe { get; init; }
    /// <summary>Package sources used instead of configured sources.</summary>
    public IReadOnlyList<string> Sources { get; init => field = Snapshot(value); } = [];
    /// <summary>The package version.</summary>
    public string? Version { get; init; }
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "nuget update",
            Arg(ConfigPath),
            Arg("-DependencyVersion", Defined(DependencyVersion, nameof(DependencyVersion))),
            Arg("-FileConflictAction", Defined(FileConflictAction, nameof(FileConflictAction))),
            Args("-Id", Ids, ","),
            Arg("-MSBuildPath", MSBuildPath),
            Arg("-MSBuildVersion", MSBuildVersion),
            Arg("-PreRelease", Prerelease),
            Arg("-RepositoryPath", RepositoryPath),
            Arg("-Safe", Safe),
            Args("-Source", Sources, " -Source "),
            Arg("-Version", Version),
            .. ConfiguredParts,
        ];
}
