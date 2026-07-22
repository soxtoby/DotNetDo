using System.Collections.ObjectModel;
using Serilog.Events;

namespace DotNetDo;

public static partial class Tools
{
    /// <summary>Creates an MSBuild command using the newest installed toolset discovered by MSBuild Locator.</summary>
    public static MSBuildCommand MSBuild => new();
}

/// <summary>Builds projects with the installed MSBuild toolset selected by MSBuild Locator.</summary>
public sealed record MSBuildCommand : ExecToolCommand
{
    /// <summary>Creates an MSBuild invocation targeting <see cref="Do.Solution"/>.</summary>
    public MSBuildCommand()
    {
        Projects = [Do.Solution.Path];
        Properties = MSBuildDefaults.Configuration is { } configuration
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Configuration"] = configuration }.AsReadOnly()
            : ReadOnlyDictionary<string, string>.Empty;
        Verbosity = MSBuildOutputVolume.Snapshot();
    }

    /// <summary>Projects or solutions to build.</summary>
    public IReadOnlyList<string> Projects { get; init => field = value.ToArray(); } = [];
    /// <summary>Targets to build, emitted as one semicolon-delimited target switch.</summary>
    public IReadOnlyList<string> Targets { get; init => field = value.ToArray(); } = [];
    /// <summary>Project-level properties to set or override.</summary>
    public IReadOnlyDictionary<string, string> Properties { get; init => field = new Dictionary<string, string>(value, StringComparer.OrdinalIgnoreCase).AsReadOnly(); } = ReadOnlyDictionary<string, string>.Empty;
    /// <summary>Amount of information written to the build log.</summary>
    public MSBuildVerbosity? Verbosity { get; init; }
    /// <summary>Maximum number of concurrent MSBuild processes; an omitted value uses one process.</summary>
    public int? MaxCpuCount { get; init; }
    /// <summary>Whether project references are restored before building.</summary>
    public bool Restore { get; init; }
    /// <summary>Whether the startup banner is hidden.</summary>
    public bool NoLogo { get; init; }
    /// <summary>Whether the default console logger is disabled.</summary>
    public bool NoConsoleLogger { get; init; }
    /// <summary>Optional binary log path; use an empty string to request the default <c>msbuild.binlog</c>.</summary>
    public string? BinaryLogger { get; init; }
    /// <summary>Optional preprocessed project output path.</summary>
    public string? Preprocess { get; init; }
    /// <summary>Whether node reuse remains enabled after the build.</summary>
    public bool? NodeReuse { get; init; }
    /// <summary>Whether interactive actions are allowed during the build.</summary>
    public bool? Interactive { get; init; }
    /// <summary>Whether project-isolation constraints are enforced.</summary>
    public bool? IsolateProjects { get; init; }
    /// <summary>Whether project-reference graphs are built concurrently.</summary>
    public bool? GraphBuild { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts =>
        [
            MSBuildToolset.MSBuild,
            Args(Projects),
            Arg("-verbosity:", Verbosity),
            Args("-target:", Targets, ";"),
            Args("-property:", Properties.Select(pair => $"{pair.Key}={pair.Value}"), ";"),
            Arg("-maxCpuCount:", MaxCpuCount),
            Arg("-restore", Restore),
            Arg("-noLogo", NoLogo),
            Arg("-noConsoleLogger", NoConsoleLogger),
            Arg("-binaryLogger:", BinaryLogger),
            Arg("-preprocess:", Preprocess),
            Arg("-nodeReuse:true", "-nodeReuse:false", NodeReuse),
            Arg("-interactive:true", "-interactive:false", Interactive),
            Arg("-isolateProjects:true", "-isolateProjects:false", IsolateProjects),
            Arg("-graphBuild:true", "-graphBuild:false", GraphBuild),
        ];
}

static class MSBuildDefaults
{
    public static string? Configuration => Do.IsLocalBuild ? null : "Release";
}

static class MSBuildOutputVolume
{
    public static MSBuildVerbosity Snapshot() => From(Logging.Level);

    public static MSBuildVerbosity From(LogEventLevel level) => level switch
        {
            LogEventLevel.Verbose => MSBuildVerbosity.Diagnostic,
            LogEventLevel.Debug => MSBuildVerbosity.Detailed,
            LogEventLevel.Information => MSBuildVerbosity.Normal,
            LogEventLevel.Warning => MSBuildVerbosity.Minimal,
            LogEventLevel.Error or LogEventLevel.Fatal => MSBuildVerbosity.Quiet,
            _ => throw new InvalidOperationException($"Unsupported logging level: {level}."),
        };
}

/// <summary>MSBuild event-log verbosity levels.</summary>
public enum MSBuildVerbosity
{
    /// <summary>No build output.</summary>
    Quiet,
    /// <summary>High-level build output.</summary>
    Minimal,
    /// <summary>Normal build output.</summary>
    Normal,
    /// <summary>Detailed build output.</summary>
    Detailed,
    /// <summary>Diagnostic build output.</summary>
    Diagnostic,
}
