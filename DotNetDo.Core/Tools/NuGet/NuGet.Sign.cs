namespace DotNetDo;
/// <summary>Signs NuGet packages.</summary>
public sealed record NuGetSign : NuGetConfiguredCommand
{
    /// <summary>Package paths or wildcard patterns processed by the command.</summary>
    public IReadOnlyList<string> Packages { get; init => field = Snapshot(value); } = [];
    /// <summary>Allows a certificate chain whose root is not trusted.</summary>
    public bool AllowUntrustedRoot { get; init; }
    /// <summary>The certificate fingerprint used to find or trust a certificate.</summary>
    public string? CertificateFingerprint { get; init; }
    /// <summary>The redacted password for the certificate file.</summary>
    public Secret? CertificatePassword { get; init; }
    /// <summary>The certificate file used for signing.</summary>
    public AbsolutePath? CertificatePath { get; init; }
    /// <summary>The Windows certificate store location searched for the signing certificate.</summary>
    public NuGetCertificateStoreLocation? CertificateStoreLocation { get; init; }
    /// <summary>The Windows certificate store name searched for the signing certificate.</summary>
    public string? CertificateStoreName { get; init; }
    /// <summary>The subject name used to find the signing certificate.</summary>
    public string? CertificateSubjectName { get; init; }
    /// <summary>The hash algorithm used for the package signature.</summary>
    public NuGetHashAlgorithm? HashAlgorithm { get; init; }
    /// <summary>The directory that receives signed packages.</summary>
    public AbsolutePath? OutputDirectory { get; init; }
    /// <summary>Replaces an existing package signature or output file.</summary>
    public bool Overwrite { get; init; }
    /// <summary>The RFC 3161 timestamp server URL.</summary>
    public string? Timestamper { get; init; }
    /// <summary>The hash algorithm used for the timestamp.</summary>
    public NuGetHashAlgorithm? TimestampHashAlgorithm { get; init; }
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "nuget sign",
            Args(RequiredPackages()),
            Arg("-AllowUntrustedRoot", AllowUntrustedRoot),
            Arg("-CertificateFingerprint", CertificateFingerprint),
            Arg("-CertificatePassword", CertificatePassword?.Unwrap()),
            Arg("-CertificatePath", CertificatePath),
            Arg("-CertificateStoreLocation", Defined(CertificateStoreLocation, nameof(CertificateStoreLocation))),
            Arg("-CertificateStoreName", CertificateStoreName),
            Arg("-CertificateSubjectName", CertificateSubjectName),
            Arg("-HashAlgorithm", Defined(HashAlgorithm, nameof(HashAlgorithm))),
            Arg("-OutputDirectory", OutputDirectory),
            Arg("-Overwrite", Overwrite),
            Arg("-Timestamper", Timestamper),
            Arg("-TimestampHashAlgorithm", Defined(TimestampHashAlgorithm, nameof(TimestampHashAlgorithm))),
            .. ConfiguredParts,
        ];
    IReadOnlyList<string> RequiredPackages() => Packages.Count > 0 && Packages.All(x => !string.IsNullOrWhiteSpace(x)) ? Packages : throw new InvalidOperationException("Packages requires at least one nonblank value.");
}
