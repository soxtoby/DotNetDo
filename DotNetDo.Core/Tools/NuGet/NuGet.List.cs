namespace DotNetDo;
/// <summary>Lists packages from configured sources.</summary>
public sealed record NuGetList : NuGetConfiguredCommand
{
    /// <summary>The optional package search text.</summary>
    public string? SearchTerm { get; init; }
    /// <summary>Lists every available version instead of only the latest.</summary>
    public bool AllVersions { get; init; }
    /// <summary>Includes unlisted package versions.</summary>
    public bool IncludeDelisted { get; init; }
    /// <summary>Includes prerelease packages.</summary>
    public bool Prerelease { get; init; }
    /// <summary>Package sources used instead of configured sources.</summary>
    public IReadOnlyList<string> Sources { get; init => field = Snapshot(value); } = [];
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "nuget list",
            Arg(SearchTerm),
            Arg("-AllVersions", AllVersions),
            Arg("-IncludeDelisted", IncludeDelisted),
            Arg("-Prerelease", Prerelease),
            Args("-Source", Sources, " -Source "),
            .. ConfiguredParts,
        ];
}
