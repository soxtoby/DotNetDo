namespace DotNetDo;
/// <summary>Searches NuGet package sources.</summary>
public sealed record NuGetSearch : NuGetCommand
{
    /// <summary>The optional package search text.</summary>
    public string? SearchTerm { get; init; }
    /// <summary>Includes prerelease packages.</summary>
    public bool Prerelease { get; init; }
    /// <summary>Package sources used instead of configured sources.</summary>
    public IReadOnlyList<string> Sources { get; init => field = Snapshot(value); } = [];
    /// <summary>The maximum number of search results returned.</summary>
    public int? Take { get; init; }
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "nuget search",
            Arg(SearchTerm),
            Arg("-Prerelease", Prerelease),
            Args("-Source", Sources, " -Source "),
            Arg("-Take", Take),
            .. VolumeParts,
        ];
}
