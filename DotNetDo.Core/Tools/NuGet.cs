using System.Collections.ObjectModel;
using Serilog.Events;

namespace DotNetDo;

public static partial class Tools
{
    /// <summary>The host-owned NuGet CLI.</summary>
    public static class NuGet
    {
        /// <summary>The canonical workspace tool-requirement name.</summary>
        public const string ToolName = "nuget";

        /// <summary>Makes <c>nuget</c> available, installing it through Scoop when missing on Windows.</summary>
        public static ToolInstall EnsureAvailable => new(ToolName, "nuget") { ScoopApp = "nuget" };
        /// <summary>Adds a package to a folder source.</summary>
        public static NuGetAdd Add => new();
        /// <summary>Reads or changes NuGet configuration.</summary>
        public static NuGetConfig Config => new();
        /// <summary>Deletes or unlists a package.</summary>
        public static NuGetDelete Delete => new();
        /// <summary>Initializes a folder source.</summary>
        public static NuGetInit Init => new();
        /// <summary>Downloads packages.</summary>
        public static NuGetInstall Install => new();
        /// <summary>Lists packages.</summary>
        public static NuGetList List => new();
        /// <summary>Manages local caches.</summary>
        public static NuGetLocals Locals => new();
        /// <summary>Creates packages.</summary>
        public static NuGetPack Pack => new();
        /// <summary>Publishes packages.</summary>
        public static NuGetPush Push => new();
        /// <summary>Restores packages.</summary>
        public static NuGetRestore Restore => new();
        /// <summary>Searches package sources.</summary>
        public static NuGetSearch Search => new();
        /// <summary>Stores a package-source API key.</summary>
        public static NuGetSetApiKey SetApiKey => new();
        /// <summary>Signs packages.</summary>
        public static NuGetSign Sign => new();
        /// <summary>Manages package sources.</summary>
        public static NuGetSources Sources => new();
        /// <summary>Creates package specifications.</summary>
        public static NuGetSpec Spec => new();
        /// <summary>Manages trusted signers.</summary>
        public static NuGetTrustedSigners TrustedSigners => new();
        /// <summary>Updates legacy package references.</summary>
        public static NuGetUpdate Update => new();
        /// <summary>Verifies package signatures.</summary>
        public static NuGetVerify Verify => new();
    }
}

/// <summary>A host-owned <c>nuget</c> invocation.</summary>
public abstract record NuGetCommand : ExecToolCommand
{
    /// <summary>Creates a command with verbosity inferred from <see cref="Logging.Level"/>.</summary>
    protected NuGetCommand() => Verbosity = NuGetOutputVolume.From(Logging.Level);

    /// <summary>Controls native NuGet output detail.</summary>
    public NuGetVerbosity? Verbosity { get; init; }
    /// <summary>Renders shared volume controls.</summary>
    protected IReadOnlyList<string?> VolumeParts => [Arg("-Verbosity", Defined(Verbosity, nameof(Verbosity)))];

    /// <summary>Requires a reference value.</summary>
    protected static T Required<T>(T? value, string name) where T : class =>
        value ?? throw new InvalidOperationException($"{name} is required.");

    /// <summary>Requires a nullable value.</summary>
    protected static T Required<T>(T? value, string name) where T : struct =>
        value ?? throw new InvalidOperationException($"{name} is required.");

    /// <summary>Requires nonblank text.</summary>
    protected static string RequiredText(string? value, string name) =>
        !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException($"{name} is required.");

    /// <summary>Rejects an undefined nullable enum value.</summary>
    protected static T? Defined<T>(T? value, string name) where T : struct, Enum =>
        value is null || Enum.IsDefined(value.Value) ? value : throw new InvalidOperationException($"{name} has an undefined value.");

    /// <summary>Rejects an undefined enum value.</summary>
    protected static T Defined<T>(T value, string name) where T : struct, Enum =>
        Enum.IsDefined(value) ? value : throw new InvalidOperationException($"{name} has an undefined value.");

    /// <summary>Snapshots a caller-owned list.</summary>
    protected static IReadOnlyList<T> Snapshot<T>(IReadOnlyList<T> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.ToArray();
    }

    /// <summary>Snapshots a caller-owned dictionary.</summary>
    protected static IReadOnlyDictionary<string, string?> Snapshot(IReadOnlyDictionary<string, string?> value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new ReadOnlyDictionary<string, string?>(new Dictionary<string, string?>(value));
    }
}

/// <summary>A NuGet command supporting an explicit configuration file.</summary>
public abstract record NuGetConfiguredCommand : NuGetCommand
{
    /// <summary>The NuGet configuration file used instead of normal discovery.</summary>
    public AbsolutePath? ConfigFile { get; init; }
    /// <summary>Forces English output.</summary>
    public bool ForceEnglishOutput { get; init; }
    /// <summary>Prevents interactive prompts.</summary>
    public bool NonInteractive { get; init; }
    /// <summary>Renders shared configuration controls.</summary>
    protected IReadOnlyList<string?> ConfiguredParts =>
        [
            Arg("-ConfigFile", ConfigFile),
            Arg("-ForceEnglishOutput", ForceEnglishOutput),
            Arg("-NonInteractive", NonInteractive),
            .. VolumeParts
        ];
}

/// <summary>NuGet output detail.</summary>
public enum NuGetVerbosity
{
    /// <summary>Writes only errors.</summary>
    Quiet,

    /// <summary>Writes normal progress and result output.</summary>
    Normal,

    /// <summary>Writes detailed diagnostic output.</summary>
    Detailed,
}

/// <summary>NuGet dependency selection behavior.</summary>
public enum NuGetDependencyVersion
{
    /// <summary>Selects the lowest dependency version that satisfies the constraint.</summary>
    Lowest,

    /// <summary>Selects the highest patch while retaining the lowest possible major and minor versions.</summary>
    HighestPatch,

    /// <summary>Selects the highest minor and patch while retaining the lowest possible major version.</summary>
    HighestMinor,

    /// <summary>Selects the highest dependency version that satisfies the constraint.</summary>
    Highest,

    /// <summary>Skips dependency resolution.</summary>
    Ignore,
}

/// <summary>Artifacts retained in the package cache.</summary>
public enum NuGetPackageSaveMode
{
    /// <summary>Saves extracted <c>.nuspec</c> files.</summary>
    Nuspec,

    /// <summary>Saves downloaded <c>.nupkg</c> files.</summary>
    Nupkg,

    /// <summary>Saves both <c>.nuspec</c> and <c>.nupkg</c> files.</summary>
    Both,
}

/// <summary>A NuGet local cache.</summary>
public enum NuGetLocalResource
{
    /// <summary>Selects every local NuGet cache.</summary>
    All,

    /// <summary>Selects the HTTP request cache.</summary>
    HttpCache,

    /// <summary>Selects the global packages folder.</summary>
    GlobalPackages,

    /// <summary>Selects NuGet's temporary-file folder.</summary>
    Temp,

    /// <summary>Selects the plugins request cache.</summary>
    PluginsCache,
}

/// <summary>An action on a NuGet local cache.</summary>
public enum NuGetLocalAction
{
    /// <summary>Deletes the selected cache contents.</summary>
    Clear,

    /// <summary>Prints the selected cache locations.</summary>
    List,
}

/// <summary>An operation on configured package sources.</summary>
public enum NuGetSourceOperation
{
    /// <summary>Lists configured package sources.</summary>
    List,

    /// <summary>Adds a package source.</summary>
    Add,

    /// <summary>Removes a package source.</summary>
    Remove,

    /// <summary>Enables a disabled package source.</summary>
    Enable,

    /// <summary>Disables a package source.</summary>
    Disable,

    /// <summary>Changes an existing package source.</summary>
    Update,
}

/// <summary>An operation on trusted signers.</summary>
public enum NuGetTrustedSignerOperation
{
    /// <summary>Lists configured trusted signers.</summary>
    List,

    /// <summary>Adds a trusted author, repository, or certificate.</summary>
    Add,

    /// <summary>Removes a trusted signer.</summary>
    Remove,

    /// <summary>Refreshes a trusted repository's certificates.</summary>
    Sync,
}

/// <summary>The package verification scope.</summary>
public enum NuGetVerificationMode
{
    /// <summary>Runs every available package verification.</summary>
    All,

    /// <summary>Verifies package signatures.</summary>
    Signatures,
}

/// <summary>Conflict handling during package updates.</summary>
public enum NuGetFileConflictAction
{
    /// <summary>Prompts before replacing a conflicting file.</summary>
    PromptUser,

    /// <summary>Overwrites conflicting files.</summary>
    Overwrite,

    /// <summary>Keeps existing conflicting files.</summary>
    Ignore,
}

/// <summary>A package-signing hash algorithm.</summary>
public enum NuGetHashAlgorithm
{
    /// <summary>Uses SHA-256.</summary>
    Sha256,

    /// <summary>Uses SHA-384.</summary>
    Sha384,

    /// <summary>Uses SHA-512.</summary>
    Sha512,
}

/// <summary>A Windows certificate store location.</summary>
public enum NuGetCertificateStoreLocation
{
    /// <summary>Uses the current user's certificate stores.</summary>
    CurrentUser,

    /// <summary>Uses the local machine's certificate stores.</summary>
    LocalMachine,
}

/// <summary>A NuGet symbol-package format.</summary>
public enum NuGetSymbolPackageFormat
{
    /// <summary>Creates an <c>.snupkg</c> symbol package.</summary>
    Snupkg,

    /// <summary>Creates a legacy <c>.symbols.nupkg</c> package.</summary>
    SymbolsNupkg,
}

/// <summary>A package-source authentication mechanism.</summary>
public enum NuGetAuthenticationType
{
    /// <summary>Uses HTTP Basic authentication.</summary>
    Basic,

    /// <summary>Negotiates the strongest supported Windows authentication protocol.</summary>
    Negotiate,

    /// <summary>Uses Kerberos authentication.</summary>
    Kerberos,

    /// <summary>Uses NTLM authentication.</summary>
    Ntlm,

    /// <summary>Uses HTTP Digest authentication.</summary>
    Digest,
}

static class NuGetOutputVolume
{
    public static NuGetVerbosity From(LogEventLevel level) => level switch
        {
            <= LogEventLevel.Debug => NuGetVerbosity.Detailed,
            >= LogEventLevel.Warning => NuGetVerbosity.Quiet,
            _ => NuGetVerbosity.Normal,
        };
}
