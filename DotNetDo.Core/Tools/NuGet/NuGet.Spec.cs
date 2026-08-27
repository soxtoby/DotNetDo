namespace DotNetDo;
/// <summary>Generates a NuGet package specification.</summary>
public sealed record NuGetSpec : NuGetCommand
{
    /// <summary>The package ID written to the generated specification.</summary>
    public string? PackageId { get; init; }
    /// <summary>The assembly used to populate package metadata.</summary>
    public AbsolutePath? AssemblyPath { get; init; }
    /// <summary>Overwrites an existing package specification.</summary>
    public bool Force { get; init; }
    /// <summary>Forces English command output.</summary>
    public bool ForceEnglishOutput { get; init; }
    /// <summary>Prevents interactive prompts.</summary>
    public bool NonInteractive { get; init; }
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "nuget spec",
            Arg(PackageId),
            Arg("-AssemblyPath", AssemblyPath),
            Arg("-Force", Force),
            Arg("-ForceEnglishOutput", ForceEnglishOutput),
            Arg("-NonInteractive", NonInteractive),
            .. VolumeParts,
        ];
}
