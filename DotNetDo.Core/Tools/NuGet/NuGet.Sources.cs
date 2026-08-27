namespace DotNetDo;
/// <summary>Manages configured NuGet package sources.</summary>
public sealed record NuGetSources : NuGetConfiguredCommand
{
    /// <summary>The operation performed by the command.</summary>
    public NuGetSourceOperation? Operation { get; init; }
    /// <summary>The package source name.</summary>
    public string? Name { get; init; }
    /// <summary>The package source name, path, or URL.</summary>
    public string? Source { get; init; }
    /// <summary>The output format used when listing sources.</summary>
    public string? Format { get; init; }
    /// <summary>The user name stored for source authentication.</summary>
    public string? UserName { get; init; }
    /// <summary>The redacted password stored for source authentication.</summary>
    public Secret? Password { get; init; }
    /// <summary>Stores the source password without encryption.</summary>
    public bool StorePasswordInClearText { get; init; }
    /// <summary>Authentication schemes allowed for the source.</summary>
    public IReadOnlyList<NuGetAuthenticationType> ValidAuthenticationTypes { get; init => field = Snapshot(value); } = [];
    /// <summary>The NuGet server protocol version used by the source.</summary>
    public int? ProtocolVersion { get; init; }
    /// <summary>Allows unencrypted HTTP package sources.</summary>
    public bool AllowInsecureConnections { get; init; }
    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            "nuget sources",
            Arg(Defined(Required(Operation, nameof(Operation)), nameof(Operation))),
            Arg("-Name", Name),
            Arg("-Source", Source),
            Arg("-Format", Format),
            Arg("-UserName", UserName),
            Arg("-Password", Password?.Unwrap()),
            Arg("-StorePasswordInClearText", StorePasswordInClearText),
            Args("-ValidAuthenticationTypes", ValidAuthenticationTypes.Select(value => Defined(value, nameof(ValidAuthenticationTypes))), ","),
            Arg("-ProtocolVersion", ProtocolVersion),
            Arg("-AllowInsecureConnections", AllowInsecureConnections),
            .. ConfiguredParts,
        ];
}
