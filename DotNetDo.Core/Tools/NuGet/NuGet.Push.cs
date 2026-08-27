namespace DotNetDo;
/// <summary>Publishes a package and optional symbols.</summary>
public sealed record NuGetPush : NuGetConfiguredCommand
{
    /// <summary>The package path or wildcard pattern to publish.</summary>
    public string? Package { get; init; }
    /// <summary>Allows unencrypted HTTP package sources.</summary>
    public bool AllowInsecureConnections { get; init; }
    /// <summary>The redacted API key sent to the package source.</summary>
    public Secret? ApiKey { get; init; }
    /// <summary>Disables request buffering while publishing.</summary>
    public bool DisableBuffering { get; init; }
    /// <summary>Uses the source URL exactly instead of appending the NuGet service endpoint.</summary>
    public bool NoServiceEndpoint { get; init; }
    /// <summary>Skips automatic publication of a matching symbols package.</summary>
    public bool NoSymbols { get; init; }
    /// <summary>The package source name, path, or URL.</summary>
    public string? Source { get; init; }
    /// <summary>Treats an already published package version as success.</summary>
    public bool SkipDuplicate { get; init; }
    /// <summary>The source that receives symbols packages.</summary>
    public string? SymbolSource { get; init; }
    /// <summary>The redacted API key sent to the symbols source.</summary>
    public Secret? SymbolApiKey { get; init; }
    /// <summary>The server timeout for the push operation.</summary>
    public TimeSpan? Timeout { get; init; }
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "nuget push",
            Arg(RequiredText(Package, nameof(Package))),
            Arg("-AllowInsecureConnections", AllowInsecureConnections),
            Arg("-ApiKey", ApiKey?.Unwrap()),
            Arg("-DisableBuffering", DisableBuffering),
            Arg("-NoServiceEndpoint", NoServiceEndpoint),
            Arg("-NoSymbols", NoSymbols),
            Arg("-Source", Source),
            Arg("-SkipDuplicate", SkipDuplicate),
            Arg("-SymbolSource", SymbolSource),
            Arg("-SymbolApiKey", SymbolApiKey?.Unwrap()),
            Arg("-Timeout", (int?)Timeout?.TotalSeconds),
            .. ConfiguredParts,
        ];
}
