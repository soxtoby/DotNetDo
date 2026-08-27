namespace DotNetDo;
/// <summary>Restores packages for legacy NuGet project formats.</summary>
public sealed record NuGetRestore : NuGetConfiguredCommand
{
    /// <summary>The solution, project, or packages.config file to restore.</summary>
    public AbsolutePath? ProjectPath { get; init; }
    /// <summary>Downloads packages without populating the HTTP or global-packages caches.</summary>
    public bool DirectDownload { get; init; }
    /// <summary>Downloads or restores packages sequentially.</summary>
    public bool DisableParallelProcessing { get; init; }
    /// <summary>Package sources consulted after the primary sources fail.</summary>
    public IReadOnlyList<string> FallbackSources { get; init => field = Snapshot(value); } = [];
    /// <summary>Restores even when the last restore is considered current.</summary>
    public bool Force { get; init; }
    /// <summary>Reevaluates every dependency even when the lock file is current.</summary>
    public bool ForceEvaluate { get; init; }
    /// <summary>The packages lock file to use.</summary>
    public AbsolutePath? LockFilePath { get; init; }
    /// <summary>Fails when restore would change the lock file.</summary>
    public bool LockedMode { get; init; }
    /// <summary>The directory containing the MSBuild executable to use.</summary>
    public AbsolutePath? MSBuildPath { get; init; }
    /// <summary>The MSBuild version to use when locating MSBuild.</summary>
    public string? MSBuildVersion { get; init; }
    /// <summary>Bypasses the HTTP request cache.</summary>
    public bool NoHttpCache { get; init; }
    /// <summary>The directory that receives command output.</summary>
    public AbsolutePath? OutputDirectory { get; init; }
    /// <summary>The package artifacts retained after download.</summary>
    public NuGetPackageSaveMode? PackageSaveMode { get; init; }
    /// <summary>The directory that receives restored packages.</summary>
    public AbsolutePath? PackagesDirectory { get; init; }
    /// <summary>The timeout for resolving project-to-project references.</summary>
    public TimeSpan? ProjectToProjectTimeout { get; init; }
    /// <summary>Restores project references recursively.</summary>
    public bool Recursive { get; init; }
    /// <summary>Checks whether package restore consent has been granted.</summary>
    public bool RequireConsent { get; init; }
    /// <summary>The solution directory used to resolve packages and repository metadata.</summary>
    public AbsolutePath? SolutionDirectory { get; init; }
    /// <summary>Package sources used instead of configured sources.</summary>
    public IReadOnlyList<string> Sources { get; init => field = Snapshot(value); } = [];
    /// <summary>Creates and uses a packages lock file.</summary>
    public bool UseLockFile { get; init; }
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "nuget restore",
            Arg(ProjectPath),
            Arg("-DirectDownload", DirectDownload),
            Arg("-DisableParallelProcessing", DisableParallelProcessing),
            Args("-FallbackSource", FallbackSources, " -FallbackSource "),
            Arg("-Force", Force),
            Arg("-ForceEvaluate", ForceEvaluate),
            Arg("-LockFilePath", LockFilePath),
            Arg("-LockedMode", LockedMode),
            Arg("-MSBuildPath", MSBuildPath),
            Arg("-MSBuildVersion", MSBuildVersion),
            Arg("-NoHttpCache", NoHttpCache),
            Arg("-OutputDirectory", OutputDirectory),
            Arg("-PackageSaveMode", SaveMode(PackageSaveMode), false),
            Arg("-PackagesDirectory", PackagesDirectory),
            Arg("-Project2ProjectTimeOut", (int?)ProjectToProjectTimeout?.TotalSeconds),
            Arg("-Recursive", Recursive),
            Arg("-RequireConsent", RequireConsent),
            Arg("-SolutionDirectory", SolutionDirectory),
            Args("-Source", Sources, " -Source "),
            Arg("-UseLockFile", UseLockFile),
            .. ConfiguredParts,
        ];
    static string? SaveMode(NuGetPackageSaveMode? value) => Defined(value, nameof(PackageSaveMode)) switch { NuGetPackageSaveMode.Both => "nuspec;nupkg", null => null, _ => value.ToString()!.ToLowerInvariant() };
}
