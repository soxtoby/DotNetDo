namespace DotNetDo;
/// <summary>Downloads packages without modifying a project.</summary>
public sealed record NuGetInstall : NuGetConfiguredCommand
{
    /// <summary>The package ID.</summary>
    public string? PackageId { get; init; }
    /// <summary>The packages.config file whose packages are downloaded.</summary>
    public AbsolutePath? PackagesConfigPath { get; init; }
    /// <summary>The package version.</summary>
    public string? Version { get; init; }
    /// <summary>The rule used to select dependency versions.</summary>
    public NuGetDependencyVersion? DependencyVersion { get; init; }
    /// <summary>Downloads packages without populating the HTTP or global-packages caches.</summary>
    public bool DirectDownload { get; init; }
    /// <summary>Downloads or restores packages sequentially.</summary>
    public bool DisableParallelProcessing { get; init; }
    /// <summary>Omits the package version from the installation directory name.</summary>
    public bool ExcludeVersion { get; init; }
    /// <summary>Package sources consulted after the primary sources fail.</summary>
    public IReadOnlyList<string> FallbackSources { get; init => field = Snapshot(value); } = [];
    /// <summary>The target framework used to choose compatible dependencies.</summary>
    public string? Framework { get; init; }
    /// <summary>Bypasses the HTTP request cache.</summary>
    public bool NoHttpCache { get; init; }
    /// <summary>The directory that receives command output.</summary>
    public AbsolutePath? OutputDirectory { get; init; }
    /// <summary>The package artifacts retained after download.</summary>
    public NuGetPackageSaveMode? PackageSaveMode { get; init; }
    /// <summary>Includes prerelease packages.</summary>
    public bool Prerelease { get; init; }
    /// <summary>Checks whether package restore consent has been granted.</summary>
    public bool RequireConsent { get; init; }
    /// <summary>The solution directory used to resolve packages and repository metadata.</summary>
    public AbsolutePath? SolutionDirectory { get; init; }
    /// <summary>Package sources used instead of configured sources.</summary>
    public IReadOnlyList<string> Sources { get; init => field = Snapshot(value); } = [];
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PackageId) && PackagesConfigPath is null) throw new InvalidOperationException("PackageId or PackagesConfigPath is required.");
            return
                [
                    "nuget install",
                    Arg(PackageId),
                    Arg(PackagesConfigPath),
                    Arg("-Version", Version),
                    Arg("-DependencyVersion", Defined(DependencyVersion, nameof(DependencyVersion))),
                    Arg("-DirectDownload", DirectDownload),
                    Arg("-DisableParallelProcessing", DisableParallelProcessing),
                    Arg("-ExcludeVersion", ExcludeVersion),
                    Args("-FallbackSource", FallbackSources, " -FallbackSource "),
                    Arg("-Framework", Framework),
                    Arg("-NoHttpCache", NoHttpCache),
                    Arg("-OutputDirectory", OutputDirectory),
                    Arg("-PackageSaveMode", SaveMode(PackageSaveMode), false),
                    Arg("-Prerelease", Prerelease),
                    Arg("-RequireConsent", RequireConsent),
                    Arg("-SolutionDirectory", SolutionDirectory),
                    Args("-Source", Sources, " -Source "),
                    .. ConfiguredParts,
                ];
        }
    }
    static string? SaveMode(NuGetPackageSaveMode? value) => Defined(value, nameof(PackageSaveMode)) switch { NuGetPackageSaveMode.Both => "nuspec;nupkg", null => null, _ => value.ToString()!.ToLowerInvariant() };
}
