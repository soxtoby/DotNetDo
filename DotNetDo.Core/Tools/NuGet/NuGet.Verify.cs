namespace DotNetDo;
/// <summary>Verifies NuGet package signatures.</summary>
public sealed record NuGetVerify : NuGetConfiguredCommand
{
    /// <summary>The verification checks applied to each package.</summary>
    public NuGetVerificationMode? Mode { get; init; }
    /// <summary>Package paths or wildcard patterns processed by the command.</summary>
    public IReadOnlyList<string> Packages { get; init => field = Snapshot(value); } = [];
    /// <summary>Certificate fingerprints accepted while verifying signatures.</summary>
    public IReadOnlyList<string> CertificateFingerprints { get; init => field = Snapshot(value); } = [];
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "nuget verify",
            Arg($"-{Defined(Required(Mode, nameof(Mode)), nameof(Mode)).ToString().ToLowerInvariant()}", true),
            Args(RequiredPackages()),
            Args("-CertificateFingerprint", CertificateFingerprints, ","),
            .. ConfiguredParts,
        ];
    IReadOnlyList<string> RequiredPackages() => Packages.Count > 0 && Packages.All(x => !string.IsNullOrWhiteSpace(x)) ? Packages : throw new InvalidOperationException("Packages requires at least one nonblank value.");
}
