namespace DotNetDo;

/// <summary>Adds a package to a hierarchical folder source.</summary>
public sealed record NuGetAdd : NuGetConfiguredCommand
{
    /// <summary>The package file added to the folder source.</summary>
    public AbsolutePath? PackagePath { get; init; }
    /// <summary>The destination folder source.</summary>
    public AbsolutePath? Source { get; init; }
    /// <summary>Copies the package contents into the folder source.</summary>
    public bool Expand { get; init; }
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "nuget add",
            Arg(Required(PackagePath, nameof(PackagePath))),
            Arg("-Source", Required(Source, nameof(Source))),
            Arg("-Expand", Expand),
            .. ConfiguredParts
        ];
}
