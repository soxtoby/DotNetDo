namespace DotNetDo;

public static partial class Tools
{
    public static partial class Azure
    {
        /// <summary>Provides fresh definitions for supported <c>az bicep</c> commands.</summary>
        public static class Bicep
        {
            /// <summary>Builds a Bicep template.</summary>
            public static AzureBicepBuild Build => new();
            /// <summary>Builds a Bicep parameters file.</summary>
            public static AzureBicepBuildParams BuildParams => new();
            /// <summary>Lints a Bicep template.</summary>
            public static AzureBicepLint Lint => new();
            /// <summary>Formats a Bicep template.</summary>
            public static AzureBicepFormat Format => new();
            /// <summary>Generates parameters for a Bicep template.</summary>
            public static AzureBicepGenerateParams GenerateParams => new();
            /// <summary>Restores external Bicep modules.</summary>
            public static AzureBicepRestore Restore => new();
            /// <summary>Publishes a Bicep module.</summary>
            public static AzureBicepPublish Publish => new();
            /// <summary>Captures or validates a Bicep deployment snapshot.</summary>
            public static AzureBicepSnapshot Snapshot => new();
            /// <summary>Installs the Bicep CLI managed by Azure CLI.</summary>
            public static AzureBicepInstall Install => new();
            /// <summary>Upgrades the Bicep CLI managed by Azure CLI.</summary>
            public static AzureBicepUpgrade Upgrade => new();
        }
    }
}

/// <summary>Models shared <c>az bicep</c> command rendering.</summary>
public abstract record AzureBicepCommand : AzureCommand
{
    /// <summary>The Bicep subcommand name.</summary>
    protected abstract string BicepCommand { get; }
    /// <summary>The canonically ordered Bicep-specific arguments.</summary>
    protected abstract IReadOnlyList<string?> BicepArguments { get; }

    /// <inheritdoc />
    protected sealed override IReadOnlyList<string?> AzureCommandParts =>
        [
            $"az bicep {BicepCommand}",
            ..BicepArguments,
        ];
}

/// <summary>Models shared file or standard-output destinations for Bicep commands.</summary>
public abstract record AzureBicepOutputCommand : AzureBicepCommand
{
    /// <summary>The directory receiving an output file whose name Bicep derives.</summary>
    public AbsolutePath? OutDirectory { get; init; }
    /// <summary>The explicit output file.</summary>
    public AbsolutePath? OutFile { get; init; }
    /// <summary>Whether generated content is written to standard output.</summary>
    public bool Stdout { get; init; }

    /// <summary>Renders and validates the output destination.</summary>
    protected IReadOnlyList<string?> OutputArguments
    {
        get
        {
            if ((OutDirectory is not null ? 1 : 0) + (OutFile is not null ? 1 : 0) + (Stdout ? 1 : 0) > 1)
                throw new InvalidOperationException("Specify at most one of OutDirectory, OutFile, or Stdout.");

            return
                [
                    Arg("--outdir", OutDirectory),
                    Arg("--outfile", OutFile),
                    Arg("--stdout", Stdout),
                ];
        }
    }
}

/// <summary>Builds a Bicep template into an ARM template.</summary>
public sealed record AzureBicepBuild : AzureBicepOutputCommand
{
    /// <summary>The Bicep template to build.</summary>
    public AbsolutePath? File { get; init; }
    /// <summary>Whether external modules must already be restored.</summary>
    public bool NoRestore { get; init; }

    /// <inheritdoc />
    protected override string BicepCommand => "build";
    /// <inheritdoc />
    protected override IReadOnlyList<string?> BicepArguments
    {
        get
        {
            ArgumentNullException.ThrowIfNull(File);
            return
                [
                    Arg("--file", File),
                    Arg("--no-restore", NoRestore),
                    ..OutputArguments,
                ];
        }
    }
}

/// <summary>Builds a Bicep parameters file into JSON parameters.</summary>
public sealed record AzureBicepBuildParams : AzureBicepOutputCommand
{
    /// <summary>The Bicep parameters file to build.</summary>
    public AbsolutePath? File { get; init; }
    /// <summary>Whether external modules must already be restored.</summary>
    public bool NoRestore { get; init; }

    /// <inheritdoc />
    protected override string BicepCommand => "build-params";
    /// <inheritdoc />
    protected override IReadOnlyList<string?> BicepArguments
    {
        get
        {
            ArgumentNullException.ThrowIfNull(File);
            return
                [
                    Arg("--file", File),
                    Arg("--no-restore", NoRestore),
                    ..OutputArguments,
                ];
        }
    }
}

/// <summary>Lints a Bicep template.</summary>
public sealed record AzureBicepLint : AzureBicepCommand
{
    /// <summary>The Bicep template to lint.</summary>
    public AbsolutePath? File { get; init; }
    /// <summary>Whether external modules must already be restored.</summary>
    public bool NoRestore { get; init; }

    /// <inheritdoc />
    protected override string BicepCommand => "lint";
    /// <inheritdoc />
    protected override IReadOnlyList<string?> BicepArguments
    {
        get
        {
            ArgumentNullException.ThrowIfNull(File);
            return
                [
                    Arg("--file", File),
                    Arg("--no-restore", NoRestore),
                ];
        }
    }
}

/// <summary>Formats a Bicep template.</summary>
public sealed record AzureBicepFormat : AzureBicepOutputCommand
{
    /// <summary>The Bicep template to format.</summary>
    public AbsolutePath? File { get; init; }
    /// <summary>The indentation style.</summary>
    public BicepIndentKind? IndentKind { get; init; }
    /// <summary>The number of spaces used for indentation.</summary>
    public int? IndentSize { get; init; }
    /// <summary>Whether a final newline is inserted.</summary>
    public bool InsertFinalNewline { get; init; }
    /// <summary>The newline style.</summary>
    public BicepNewlineKind? NewlineKind { get; init; }

    /// <inheritdoc />
    protected override string BicepCommand => "format";
    /// <inheritdoc />
    protected override IReadOnlyList<string?> BicepArguments
    {
        get
        {
            ArgumentNullException.ThrowIfNull(File);
            if (IndentSize is not null && IndentKind != BicepIndentKind.Space)
                throw new InvalidOperationException("IndentSize requires IndentKind.Space.");

            return
                [
                    Arg("--file", File),
                    Arg("--indent-kind", IndentKind?.ToString().ToPascalCase()),
                    Arg("--indent-size", IndentSize),
                    Arg("--insert-final-newline", InsertFinalNewline),
                    Arg("--newline-kind", NewlineKind?.ToString().ToSnakeCaseUpper()),
                    ..OutputArguments,
                ];
        }
    }
}

/// <summary>Generates a parameters file from a Bicep template.</summary>
public sealed record AzureBicepGenerateParams : AzureBicepOutputCommand
{
    /// <summary>The Bicep template used to generate parameters.</summary>
    public AbsolutePath? File { get; init; }
    /// <summary>Which template parameters are included.</summary>
    public BicepIncludedParameters? IncludeParameters { get; init; }
    /// <summary>Whether external modules must already be restored.</summary>
    public bool NoRestore { get; init; }
    /// <summary>The generated parameter-file format.</summary>
    public BicepParameterOutputFormat? OutputFormat { get; init; }

    /// <inheritdoc />
    protected override string BicepCommand => "generate-params";
    /// <inheritdoc />
    protected override IReadOnlyList<string?> BicepArguments
    {
        get
        {
            ArgumentNullException.ThrowIfNull(File);
            return
                [
                    Arg("--file", File),
                    Arg("--include-params", IncludeParameters is null ? null 
                        : IncludeParameters.Value is BicepIncludedParameters.All ? "all"
                        : IncludeParameters.Value.ToString().ToPascalCase()),
                    Arg("--no-restore", NoRestore),
                    Arg("--output-format", OutputFormat),
                    ..OutputArguments,
                ];
        }
    }
}

/// <summary>Restores external modules referenced by a Bicep template.</summary>
public sealed record AzureBicepRestore : AzureBicepCommand
{
    /// <summary>The Bicep template whose modules are restored.</summary>
    public AbsolutePath? File { get; init; }
    /// <summary>Whether cached modules are overwritten.</summary>
    public bool Force { get; init; }

    /// <inheritdoc />
    protected override string BicepCommand => "restore";
    /// <inheritdoc />
    protected override IReadOnlyList<string?> BicepArguments
    {
        get
        {
            ArgumentNullException.ThrowIfNull(File);
            return
                [
                    Arg("--file", File),
                    Arg("--force", Force),
                ];
        }
    }
}

/// <summary>Publishes a Bicep module to a registry.</summary>
public sealed record AzureBicepPublish : AzureBicepCommand
{
    /// <summary>The Bicep module file to publish.</summary>
    public AbsolutePath? File { get; init; }
    /// <summary>The Bicep registry target reference.</summary>
    public string? Target { get; init; }
    /// <summary>The published module documentation URI.</summary>
    public Uri? DocumentationUri { get; init; }
    /// <summary>Whether an existing target is overwritten.</summary>
    public bool Force { get; init; }
    /// <summary>Whether source code is included.</summary>
    public bool WithSource { get; init; }

    /// <inheritdoc />
    protected override string BicepCommand => "publish";
    /// <inheritdoc />
    protected override IReadOnlyList<string?> BicepArguments
    {
        get
        {
            ArgumentNullException.ThrowIfNull(File);
            ArgumentException.ThrowIfNullOrWhiteSpace(Target);
            return
                [
                    Arg("--file", File),
                    Arg("--target", Target),
                    Arg("--documentation-uri", DocumentationUri),
                    Arg("--force", Force),
                    Arg("--with-source", WithSource),
                ];
        }
    }
}

/// <summary>Captures or validates resources predicted by a Bicep parameters file.</summary>
public sealed record AzureBicepSnapshot : AzureBicepCommand
{
    /// <summary>The Bicep parameters file used for the snapshot.</summary>
    public AbsolutePath? File { get; init; }
    /// <summary>The deployment name used for prediction.</summary>
    public string? DeploymentName { get; init; }
    /// <summary>The Azure location used for prediction.</summary>
    public string? Location { get; init; }
    /// <summary>The management-group scope.</summary>
    public string? ManagementGroupId { get; init; }
    /// <summary>Whether the snapshot is captured or validated.</summary>
    public BicepSnapshotMode? Mode { get; init; }
    /// <summary>The resource-group scope.</summary>
    public string? ResourceGroup { get; init; }
    /// <summary>The subscription scope.</summary>
    public Guid? SubscriptionId { get; init; }
    /// <summary>The tenant scope.</summary>
    public Guid? TenantId { get; init; }

    /// <inheritdoc />
    protected override string BicepCommand => "snapshot";
    /// <inheritdoc />
    protected override IReadOnlyList<string?> BicepArguments
    {
        get
        {
            ArgumentNullException.ThrowIfNull(File);
            return
                [
                    Arg("--file", File),
                    Arg("--deployment-name", DeploymentName),
                    Arg("--location", Location),
                    Arg("--management-group-id", ManagementGroupId),
                    Arg("--mode", Mode?.ToString().ToPascalCase()),
                    Arg("--resource-group", ResourceGroup),
                    Arg("--subscription-id", SubscriptionId),
                    Arg("--tenant-id", TenantId),
                ];
        }
    }
}

/// <summary>Installs the Bicep CLI managed by Azure CLI.</summary>
public sealed record AzureBicepInstall : AzureBicepCommand
{
    /// <summary>The target platform, or automatic platform detection when omitted.</summary>
    public BicepTargetPlatform? TargetPlatform { get; init; }
    /// <summary>The Bicep version, including an optional <c>v</c> prefix or prerelease label.</summary>
    public string? Version { get; init; }

    /// <inheritdoc />
    protected override string BicepCommand => "install";
    /// <inheritdoc />
    protected override IReadOnlyList<string?> BicepArguments =>
        [
            Arg("--target-platform", TargetPlatform?.ToString().ToKebabCaseLower()),
            Arg("--version", Version),
        ];
}

/// <summary>Upgrades the Bicep CLI managed by Azure CLI.</summary>
public sealed record AzureBicepUpgrade : AzureBicepCommand
{
    /// <summary>The target platform, or automatic platform detection when omitted.</summary>
    public BicepTargetPlatform? TargetPlatform { get; init; }

    /// <inheritdoc />
    protected override string BicepCommand => "upgrade";
    /// <inheritdoc />
    protected override IReadOnlyList<string?> BicepArguments =>
        [
            Arg("--target-platform", TargetPlatform?.ToString().ToKebabCaseLower()),
        ];
}

/// <summary>Bicep indentation styles.</summary>
public enum BicepIndentKind
{
    /// <summary>Space indentation.</summary>
    Space,
    /// <summary>Tab indentation.</summary>
    Tab,
}

/// <summary>Bicep newline styles.</summary>
public enum BicepNewlineKind
{
    /// <summary>Carriage return.</summary>
    CR,
    /// <summary>Carriage return followed by line feed.</summary>
    CRLF,
    /// <summary>Line feed.</summary>
    LF,
}

/// <summary>Sets of parameters included by Bicep parameter generation.</summary>
public enum BicepIncludedParameters
{
    /// <summary>All parameters.</summary>
    All,
    /// <summary>Only parameters without defaults.</summary>
    RequiredOnly,
}

/// <summary>Bicep generated parameter-file formats.</summary>
public enum BicepParameterOutputFormat
{
    /// <summary>JSON parameters.</summary>
    Json,
    /// <summary>Bicep parameters.</summary>
    BicepParam,
}

/// <summary>Bicep deployment snapshot operations.</summary>
public enum BicepSnapshotMode
{
    /// <summary>Capture or replace the snapshot.</summary>
    Overwrite,
    /// <summary>Validate the existing snapshot.</summary>
    Validate,
}

/// <summary>Platforms supported by Azure CLI's Bicep installer.</summary>
public enum BicepTargetPlatform
{
    /// <summary>Linux ARM64.</summary>
    LinuxArm64,
    /// <summary>Linux musl x64.</summary>
    LinuxMuslX64,
    /// <summary>Linux x64.</summary>
    LinuxX64,
    /// <summary>macOS ARM64.</summary>
    OsxArm64,
    /// <summary>macOS x64.</summary>
    OsxX64,
    /// <summary>Windows ARM64.</summary>
    WinArm64,
    /// <summary>Windows x64.</summary>
    WinX64,
}