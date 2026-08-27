namespace DotNetDo;
/// <summary>Creates a NuGet package from a specification or legacy project.</summary>
public sealed record NuGetPack : NuGetConfiguredCommand
{
    /// <summary>The .nuspec or legacy project file to pack.</summary>
    public AbsolutePath? InputPath { get; init; }
    /// <summary>The base directory used to resolve files declared by the package specification.</summary>
    public AbsolutePath? BasePath { get; init; }
    /// <summary>Builds the project before packing it.</summary>
    public bool Build { get; init; }
    /// <summary>Produces a package with deterministic timestamps and metadata.</summary>
    public bool Deterministic { get; init; }
    /// <summary>The timestamp written when deterministic packing is enabled.</summary>
    public DateTimeOffset? DeterministicTimestamp { get; init; }
    /// <summary>Wildcard patterns for files omitted from the package.</summary>
    public IReadOnlyList<string> Exclude { get; init => field = Snapshot(value); } = [];
    /// <summary>Omits empty directories from the package.</summary>
    public bool ExcludeEmptyDirectories { get; init; }
    /// <summary>Includes referenced projects as dependencies or package contents.</summary>
    public bool IncludeReferencedProjects { get; init; }
    /// <summary>Installs the produced package into the output directory.</summary>
    public bool InstallPackageToOutputPath { get; init; }
    /// <summary>The minimum NuGet client version required to install the package.</summary>
    public string? MinClientVersion { get; init; }
    /// <summary>The directory containing the MSBuild executable to use.</summary>
    public AbsolutePath? MSBuildPath { get; init; }
    /// <summary>The MSBuild version to use when locating MSBuild.</summary>
    public string? MSBuildVersion { get; init; }
    /// <summary>Includes files normally excluded by NuGet defaults.</summary>
    public bool NoDefaultExcludes { get; init; }
    /// <summary>Skips package analysis after packing.</summary>
    public bool NoPackageAnalysis { get; init; }
    /// <summary>The directory that receives command output.</summary>
    public AbsolutePath? OutputDirectory { get; init; }
    /// <summary>Omits the version from generated package filenames.</summary>
    public bool OutputFileNamesWithoutVersion { get; init; }
    /// <summary>The packages directory used to resolve referenced projects.</summary>
    public AbsolutePath? PackagesDirectory { get; init; }
    /// <summary>The solution directory used to resolve packages and repository metadata.</summary>
    public AbsolutePath? SolutionDirectory { get; init; }
    /// <summary>The suffix appended to the package version.</summary>
    public string? Suffix { get; init; }
    /// <summary>The format used for the symbols package.</summary>
    public NuGetSymbolPackageFormat? SymbolPackageFormat { get; init; }
    /// <summary>Creates a symbols package alongside the main package.</summary>
    public bool Symbols { get; init; }
    /// <summary>Creates a package intended for the legacy tools folder convention.</summary>
    public bool Tool { get; init; }
    /// <summary>The package version.</summary>
    public string? Version { get; init; }
    /// <summary>MSBuild properties passed to the pack operation. Null values render as empty values.</summary>
    public IReadOnlyDictionary<string, string?> Properties { get; init => field = Snapshot(value); } = new Dictionary<string, string?>().AsReadOnly();
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "nuget pack",
            Arg(Required(InputPath, nameof(InputPath))),
            Arg("-BasePath", BasePath),
            Arg("-Build", Build),
            Arg("-Deterministic", Deterministic),
            Arg("-DeterministicTimestamp", DeterministicTimestamp?.ToString("O")),
            Args("-Exclude", Exclude, " -Exclude "),
            Arg("-ExcludeEmptyDirectories", ExcludeEmptyDirectories),
            Arg("-IncludeReferencedProjects", IncludeReferencedProjects),
            Arg("-InstallPackageToOutputPath", InstallPackageToOutputPath),
            Arg("-MinClientVersion", MinClientVersion),
            Arg("-MSBuildPath", MSBuildPath),
            Arg("-MSBuildVersion", MSBuildVersion),
            Arg("-NoDefaultExcludes", NoDefaultExcludes),
            Arg("-NoPackageAnalysis", NoPackageAnalysis),
            Arg("-OutputDirectory", OutputDirectory),
            Arg("-OutputFileNamesWithoutVersion", OutputFileNamesWithoutVersion),
            Arg("-PackagesDirectory", PackagesDirectory),
            Arg("-SolutionDirectory", SolutionDirectory),
            Arg("-Suffix", Suffix),
            Arg("-SymbolPackageFormat", SymbolFormat(SymbolPackageFormat)),
            Arg("-Symbols", Symbols),
            Arg("-Tool", Tool),
            Arg("-Version", Version),
            .. ConfiguredParts,
            Arg("-Properties", Properties.Count == 0 ? null : string.Join(';', Properties.Select(x => $"{x.Key}={x.Value ?? ""}"))),
        ];
    static string? SymbolFormat(NuGetSymbolPackageFormat? value) => Defined(value, nameof(SymbolPackageFormat)) switch { NuGetSymbolPackageFormat.Snupkg => "snupkg", NuGetSymbolPackageFormat.SymbolsNupkg => "symbols.nupkg", _ => null };
}
