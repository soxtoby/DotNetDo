namespace DotNetDo;
/// <summary>Stores an API key for a package source.</summary>
public sealed record NuGetSetApiKey : NuGetConfiguredCommand
{
    /// <summary>The redacted API key sent to the package source.</summary>
    public Secret? ApiKey { get; init; }
    /// <summary>The package source associated with the stored API key.</summary>
    public string? Source { get; init; }
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "nuget setapikey",
            Arg(Required(ApiKey, nameof(ApiKey)).Unwrap()),
            Arg("-Source", Source),
            .. ConfiguredParts,
        ];
}
