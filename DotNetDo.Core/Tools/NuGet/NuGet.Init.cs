namespace DotNetDo;
/// <summary>Copies packages into a hierarchical folder source.</summary>
public sealed record NuGetInit : NuGetConfiguredCommand
{
    /// <summary>The folder containing packages to copy.</summary>
    public AbsolutePath? Source { get; init; }
    /// <summary>The folder source that receives the packages.</summary>
    public AbsolutePath? Destination { get; init; }
    /// <summary>Copies package contents into the destination hierarchy.</summary>
    public bool Expand { get; init; }
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "nuget init",
            Arg(Required(Source, nameof(Source))),
            Arg(Required(Destination, nameof(Destination))),
            Arg("-Expand", Expand),
            .. ConfiguredParts,
        ];
}
