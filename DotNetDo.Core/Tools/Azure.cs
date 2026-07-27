using Serilog.Events;

namespace DotNetDo;

public static partial class Tools
{
    /// <summary>The Azure CLI for managing Azure resources from a terminal.</summary>
    public static partial class Azure
    {
        internal const string ToolName = "azure";
        
        /// <summary>Makes the <c>az</c> command available.</summary>
        public static ToolInstall EnsureAvailable => new(ToolName, "az") { ScoopApp = "azure-cli" };
    }
}

/// <summary>Models shared Azure CLI options and canonical rendering.</summary>
public abstract record AzureCommand : ExecToolCommand
{
    /// <summary>Initializes Azure CLI output controls from the task logging level.</summary>
    protected AzureCommand()
    {
        (Debug, Verbose, OnlyShowErrors) = Logging.Level switch
            {
                LogEventLevel.Verbose => (true, false, false),
                LogEventLevel.Debug => (false, true, false),
                >= LogEventLevel.Warning => (false, false, true),
                _ => (false, false, false),
            };
    }

    /// <summary>Whether all Azure CLI debug logs are shown.</summary>
    public bool Debug { get; init; }
    /// <summary>Whether additional Azure CLI operational details are shown.</summary>
    public bool Verbose { get; init; }
    /// <summary>Whether Azure CLI warnings are suppressed.</summary>
    public bool OnlyShowErrors { get; init; }
    /// <summary>The subscription name or ID used by Azure CLI.</summary>
    public string? Subscription { get; init; }
    /// <summary>The Azure CLI output format.</summary>
    public AzureOutputFormat? Output { get; init; }
    /// <summary>The JMESPath query applied by Azure CLI.</summary>
    public string? Query { get; init; }

    /// <summary>Gets the command-specific parts rendered before global Azure CLI options.</summary>
    protected abstract IReadOnlyList<string?> AzureCommandParts { get; }

    /// <inheritdoc />
    protected sealed override IReadOnlyList<string?> CommandParts =>
        [
            ..AzureCommandParts,
            Arg("--debug", Debug),
            Arg("--verbose", Verbose),
            Arg("--only-show-errors", OnlyShowErrors),
            Arg("--subscription", Subscription),
            Arg("--output", Output),
            Arg("--query", Query),
        ];
}

/// <summary>Azure CLI output formats.</summary>
public enum AzureOutputFormat
{
    /// <summary>JSON.</summary>
    Json,
    /// <summary>Colorized JSON.</summary>
    JsonC,
    /// <summary>No output.</summary>
    None,
    /// <summary>Human-readable table.</summary>
    Table,
    /// <summary>Tab-separated values.</summary>
    Tsv,
    /// <summary>YAML.</summary>
    Yaml,
    /// <summary>Colorized YAML.</summary>
    YamlC,
}
