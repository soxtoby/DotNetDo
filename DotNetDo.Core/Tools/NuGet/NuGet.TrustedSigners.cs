namespace DotNetDo;
/// <summary>Manages NuGet trusted signers.</summary>
public sealed record NuGetTrustedSigners : NuGetConfiguredCommand
{
    /// <summary>The operation performed by the command.</summary>
    public NuGetTrustedSignerOperation Operation { get; init; } = NuGetTrustedSignerOperation.List;
    /// <summary>The trusted signer entry name.</summary>
    public string? Name { get; init; }
    /// <summary>A signed package used to identify the author or repository certificate.</summary>
    public string? Package { get; init; }
    /// <summary>Adds the package author as the trusted signer.</summary>
    public bool Author { get; init; }
    /// <summary>Adds the package repository as the trusted signer.</summary>
    public bool Repository { get; init; }
    /// <summary>Allows a certificate chain whose root is not trusted.</summary>
    public bool AllowUntrustedRoot { get; init; }
    /// <summary>Repository owners allowed by the trusted signer.</summary>
    public IReadOnlyList<string> Owners { get; init => field = Snapshot(value); } = [];
    /// <summary>The repository service-index URL.</summary>
    public string? ServiceIndex { get; init; }
    /// <summary>The certificate fingerprint used to find or trust a certificate.</summary>
    public string? CertificateFingerprint { get; init; }
    /// <summary>The hash algorithm used by the certificate fingerprint.</summary>
    public NuGetHashAlgorithm? FingerprintAlgorithm { get; init; }
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "nuget trusted-signers",
            Arg(Defined(Operation, nameof(Operation))),
            Arg("-Name", Name),
            Arg(Package),
            Arg("-Author", Author),
            Arg("-Repository", Repository),
            Arg("-AllowUntrustedRoot", AllowUntrustedRoot),
            Args("-Owners", Owners, ","),
            Arg("-ServiceIndex", ServiceIndex),
            Arg("-CertificateFingerprint", CertificateFingerprint),
            Arg("-FingerprintAlgorithm", Defined(FingerprintAlgorithm, nameof(FingerprintAlgorithm))),
            .. ConfiguredParts,
        ];
}
