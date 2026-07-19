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
        Verbosity = MSBuildOutputVolume.Snapshot();
    }

    /// <summary>Projects or solutions to build.</summary>
    public IReadOnlyList<string> Projects { get => GetArgumentArray("projects"); init => SetArgumentArray("projects", "", value); }
    /// <summary>Targets to build, emitted as one semicolon-delimited target switch.</summary>
    public IReadOnlyList<string> Targets { get => GetArgumentArray("targets"); init => SetArgumentArray("targets", "-target:", value, ";"); }
    /// <summary>Project-level properties to set or override.</summary>
    public IReadOnlyDictionary<string, string> Properties { get => GetArgumentDictionary("properties"); init => SetArgumentDictionary("properties", "-property:", value, ";", comparer: StringComparer.OrdinalIgnoreCase); }
    /// <summary>Amount of information written to the build log.</summary>
    public MSBuildVerbosity? Verbosity { get => GetEnum<MSBuildVerbosity>("verbosity"); init => SetEnum("verbosity", "-verbosity:", value); }
    /// <summary>Maximum number of concurrent MSBuild processes; an omitted value uses one process.</summary>
    public int? MaxCpuCount { get => GetInt("max-cpu-count"); init => SetInt("max-cpu-count", "-maxCpuCount:", value); }
    /// <summary>Whether project references are restored before building.</summary>
    public bool Restore { get => GetFlag("restore"); init => SetFlag("restore", "-restore", value); }
    /// <summary>Whether the startup banner is hidden.</summary>
    public bool NoLogo { get => GetFlag("no-logo"); init => SetFlag("no-logo", "-noLogo", value); }
    /// <summary>Whether the default console logger is disabled.</summary>
    public bool NoConsoleLogger { get => GetFlag("no-console-logger"); init => SetFlag("no-console-logger", "-noConsoleLogger", value); }
    /// <summary>Optional binary log path; use an empty string to request the default <c>msbuild.binlog</c>.</summary>
    public string? BinaryLogger { get => GetArgument("binary-logger"); init => SetArgument("binary-logger", "-binaryLogger:", value); }
    /// <summary>Optional preprocessed project output path.</summary>
    public string? Preprocess { get => GetArgument("preprocess"); init => SetArgument("preprocess", "-preprocess:", value); }
    /// <summary>Whether node reuse remains enabled after the build.</summary>
    public bool? NodeReuse { get => GetFlag("node-reuse", "-nodeReuse:true", "-nodeReuse:false"); init => SetFlag("node-reuse", "-nodeReuse:true", "-nodeReuse:false", value); }
    /// <summary>Whether interactive actions are allowed during the build.</summary>
    public bool? Interactive { get => GetFlag("interactive", "-interactive:true", "-interactive:false"); init => SetFlag("interactive", "-interactive:true", "-interactive:false", value); }
    /// <summary>Whether project-isolation constraints are enforced.</summary>
    public bool? IsolateProjects { get => GetFlag("isolate-projects", "-isolateProjects:true", "-isolateProjects:false"); init => SetFlag("isolate-projects", "-isolateProjects:true", "-isolateProjects:false", value); }
    /// <summary>Whether project-reference graphs are built concurrently.</summary>
    public bool? GraphBuild { get => GetFlag("graph-build", "-graphBuild:true", "-graphBuild:false"); init => SetFlag("graph-build", "-graphBuild:true", "-graphBuild:false", value); }

    /// <summary>The located MSBuild executable, or the .NET host plus the located SDK MSBuild assembly.</summary>
    protected override string CommandPrefix => MSBuildToolset.MSBuild;

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
