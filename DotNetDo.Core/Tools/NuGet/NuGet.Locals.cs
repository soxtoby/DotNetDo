namespace DotNetDo;
/// <summary>Lists or clears local NuGet caches.</summary>
public sealed record NuGetLocals : NuGetConfiguredCommand
{
    /// <summary>The local NuGet cache to inspect or clear.</summary>
    public NuGetLocalResource? Resource { get; init; }
    /// <summary>Whether to list or clear the selected cache.</summary>
    public NuGetLocalAction? Action { get; init; }
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "nuget locals",
            Arg(Render(Defined(Required(Resource, nameof(Resource)), nameof(Resource)))),
            Arg($"-{Render(Defined(Required(Action, nameof(Action)), nameof(Action)))}", true),
            .. ConfiguredParts,
        ];
    static string Render(NuGetLocalResource value) => value switch { NuGetLocalResource.HttpCache => "http-cache", NuGetLocalResource.GlobalPackages => "global-packages", NuGetLocalResource.PluginsCache => "plugins-cache", _ => value.ToString().ToLowerInvariant() };
    static string Render(NuGetLocalAction value) => value.ToString().ToLowerInvariant();
}
