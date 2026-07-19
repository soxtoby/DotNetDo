namespace DotNetDo;

public static partial class Tools
{
    /// <summary>Creates a VSTest command using the runner supplied by the newest installed MSBuild toolset.</summary>
    public static VSTestCommand VSTest => new();
}

/// <summary>Runs test containers with the VSTest runner selected by MSBuild Locator.</summary>
public sealed record VSTestCommand : ExecToolCommand
{
    /// <summary>Test containers or wildcard patterns to run.</summary>
    public IReadOnlyList<string> TestFiles { get => GetArgumentArray("test-files"); init => SetArgumentArray("test-files", "", value); }
    /// <summary>Test names to run, emitted as one comma-delimited option; cannot be combined with <see cref="TestCaseFilter"/>.</summary>
    public IReadOnlyList<string> Tests { get => GetArgumentArray("tests"); init => SetArgumentArray("tests", "--Tests:", value, ","); }
    /// <summary>Test-case filter expression; cannot be combined with <see cref="Tests"/>.</summary>
    public string? TestCaseFilter { get => GetArgument("test-case-filter"); init => SetArgument("test-case-filter", "--TestCaseFilter:", value); }
    /// <summary>Target framework used for test execution.</summary>
    public string? Framework { get => GetArgument("framework"); init => SetArgument("framework", "--Framework:", value); }
    /// <summary>Target process architecture used for test execution.</summary>
    public VSTestPlatform? Platform { get => GetEnum<VSTestPlatform>("platform"); init => SetEnum("platform", "--Platform:", value); }
    /// <summary>Environment variables set for the test run; specifying any variable implies isolation.</summary>
    public IReadOnlyDictionary<string, string> Environment { get => GetArgumentDictionary("environment"); init => SetArgumentDictionary("environment", "-e:", value, " -e:", comparer: StringComparer.OrdinalIgnoreCase); }
    /// <summary>Run-settings file used for test execution.</summary>
    public string? Settings { get => GetArgument("settings"); init => SetArgument("settings", "--Settings:", value); }
    /// <summary>Whether discovered tests are listed instead of run.</summary>
    public bool ListTests { get => GetFlag("list-tests"); init => SetFlag("list-tests", "--ListTests", value); }
    /// <summary>Whether tests run in parallel using up to all available cores.</summary>
    public bool Parallel { get => GetFlag("parallel"); init => SetFlag("parallel", "--Parallel", value); }
    /// <summary>Path searched for custom test adapters.</summary>
    public string? TestAdapterPath { get => GetArgument("test-adapter-path"); init => SetArgument("test-adapter-path", "--TestAdapterPath:", value); }
    /// <summary>Test-adapter loading strategy accepted by the installed runner.</summary>
    public string? TestAdapterLoadingStrategy { get => GetArgument("test-adapter-loading-strategy"); init => SetArgument("test-adapter-loading-strategy", "--TestAdapterLoadingStrategy:", value); }
    /// <summary>Whether blame data is collected to diagnose a crashing test host.</summary>
    public bool Blame { get => GetFlag("blame"); init => SetFlag("blame", "--Blame", value); }
    /// <summary>Diagnostic log path, optionally followed by a semicolon-delimited trace level.</summary>
    public string? Diag { get => GetArgument("diag"); init => SetArgument("diag", "--Diag:", value); }
    /// <summary>Logger names and settings; each value emits a separate logger option.</summary>
    public IReadOnlyList<string> Loggers { get => GetArgumentArray("loggers"); init => SetArgumentArray("loggers", "--Logger:", value, " --Logger:"); }
    /// <summary>Directory created for test results.</summary>
    public string? ResultsDirectory { get => GetArgument("results-directory"); init => SetArgument("results-directory", "--ResultsDirectory:", value); }
    /// <summary>Parent process ID used by an orchestrating host.</summary>
    public int? ParentProcessId { get => GetInt("parent-process-id"); init => SetInt("parent-process-id", "--ParentProcessId:", value); }
    /// <summary>Socket port used by an orchestrating host.</summary>
    public int? Port { get => GetInt("port"); init => SetInt("port", "--Port:", value); }
    /// <summary>Data collectors enabled for the test run; each value emits a separate collect option.</summary>
    public IReadOnlyList<string> Collect { get => GetArgumentArray("collect"); init => SetArgumentArray("collect", "--Collect:", value, " --Collect:"); }
    /// <summary>Whether tests run in an isolated process.</summary>
    public bool InIsolation { get => GetFlag("in-isolation"); init => SetFlag("in-isolation", "--InIsolation", value); }

    /// <summary>The VSTest executable, or the .NET host plus the located SDK VSTest assembly.</summary>
    protected override string CommandPrefix
    {
        get
        {
            if (Tests.Count != 0 && !string.IsNullOrWhiteSpace(TestCaseFilter))
                throw new InvalidOperationException($"{nameof(Tests)} and {nameof(TestCaseFilter)} cannot be combined.");

            return MSBuildToolset.VSTest;
        }
    }
}

/// <summary>Process architectures supported by VSTest's platform option.</summary>
public enum VSTestPlatform
{
    /// <summary>32-bit x86.</summary>
    X86,
    /// <summary>64-bit x86.</summary>
    X64,
    /// <summary>ARM; runner support depends on the installed toolset.</summary>
    Arm,
}
