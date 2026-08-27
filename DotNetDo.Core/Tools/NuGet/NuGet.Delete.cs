namespace DotNetDo;
/// <summary>Deletes or unlists a package version from a source.</summary>
public sealed record NuGetDelete : NuGetConfiguredCommand
{
    /// <summary>The package ID.</summary>
    public string? PackageId { get; init; }
    /// <summary>The package version.</summary>
    public string? PackageVersion { get; init; }
    /// <summary>The redacted API key sent to the package source.</summary>
    public Secret? ApiKey { get; init; }
    /// <summary>The package source name, path, or URL.</summary>
    public string? Source { get; init; }
    /// <summary>Uses the source URL exactly instead of appending the NuGet service endpoint.</summary>
    public bool NoServiceEndpoint { get; init; }
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "nuget delete",
            Arg(RequiredText(PackageId, nameof(PackageId))),
            Arg(RequiredText(PackageVersion, nameof(PackageVersion))),
            Arg("-ApiKey", ApiKey?.Unwrap()),
            Arg("-Source", Source),
            Arg("-NoServiceEndpoint", NoServiceEndpoint),
            .. ConfiguredParts,
        ];
}
