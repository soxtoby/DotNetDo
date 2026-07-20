using System.Collections.ObjectModel;

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
    public IReadOnlyList<string> TestFiles { get; init => field = value.ToArray(); } = [];
    /// <summary>Test names to run, emitted as one comma-delimited option; cannot be combined with <see cref="TestCaseFilter"/>.</summary>
    public IReadOnlyList<string> Tests { get; init => field = value.ToArray(); } = [];
    /// <summary>Test-case filter expression; cannot be combined with <see cref="Tests"/>.</summary>
    public string? TestCaseFilter { get; init; }
    /// <summary>Target framework used for test execution.</summary>
    public string? Framework { get; init; }
    /// <summary>Target process architecture used for test execution.</summary>
    public VSTestPlatform? Platform { get; init; }
    /// <summary>Environment variables set for the test run; specifying any variable implies isolation.</summary>
    public IReadOnlyDictionary<string, string> Environment { get; init => field = new Dictionary<string, string>(value, StringComparer.OrdinalIgnoreCase).AsReadOnly(); } = ReadOnlyDictionary<string, string>.Empty;
    /// <summary>Run-settings file used for test execution.</summary>
    public string? Settings { get; init; }
    /// <summary>Whether discovered tests are listed instead of run.</summary>
    public bool ListTests { get; init; }
    /// <summary>Whether tests run in parallel using up to all available cores.</summary>
    public bool Parallel { get; init; }
    /// <summary>Path searched for custom test adapters.</summary>
    public string? TestAdapterPath { get; init; }
    /// <summary>Test-adapter loading strategy accepted by the installed runner.</summary>
    public string? TestAdapterLoadingStrategy { get; init; }
    /// <summary>Whether blame data is collected to diagnose a crashing test host.</summary>
    public bool Blame { get; init; }
    /// <summary>Diagnostic log path, optionally followed by a semicolon-delimited trace level.</summary>
    public string? Diag { get; init; }
    /// <summary>Logger names and settings; each value emits a separate logger option.</summary>
    public IReadOnlyList<string> Loggers { get; init => field = value.ToArray(); } = [];
    /// <summary>Directory created for test results.</summary>
    public string? ResultsDirectory { get; init; }
    /// <summary>Parent process ID used by an orchestrating host.</summary>
    public int? ParentProcessId { get; init; }
    /// <summary>Socket port used by an orchestrating host.</summary>
    public int? Port { get; init; }
    /// <summary>Data collectors enabled for the test run; each value emits a separate collect option.</summary>
    public IReadOnlyList<string> Collect { get; init => field = value.ToArray(); } = [];
    /// <summary>Whether tests run in an isolated process.</summary>
    public bool InIsolation { get; init; }

    /// <inheritdoc />
    protected override IReadOnlyList<string?> CommandParts
    {
        get
        {
            if (Tests.Count != 0 && !string.IsNullOrWhiteSpace(TestCaseFilter))
                throw new InvalidOperationException($"{nameof(Tests)} and {nameof(TestCaseFilter)} cannot be combined.");

            return
                [
                    MSBuildToolset.VSTest,
                    Args(TestFiles),
                    Args("--Tests:", Tests, ","),
                    Arg("--TestCaseFilter:", TestCaseFilter),
                    Arg("--Framework:", Framework),
                    Arg("--Platform:", Platform),
                    Args("-e:", Environment.Select(pair => $"{pair.Key}={pair.Value}"), " -e:"),
                    Arg("--Settings:", Settings),
                    Arg("--ListTests", ListTests),
                    Arg("--Parallel", Parallel),
                    Arg("--TestAdapterPath:", TestAdapterPath),
                    Arg("--TestAdapterLoadingStrategy:", TestAdapterLoadingStrategy),
                    Arg("--Blame", Blame),
                    Arg("--Diag:", Diag),
                    Args("--Logger:", Loggers, " --Logger:"),
                    Arg("--ResultsDirectory:", ResultsDirectory),
                    Arg("--ParentProcessId:", ParentProcessId),
                    Arg("--Port:", Port),
                    Args("--Collect:", Collect, " --Collect:"),
                    Arg("--InIsolation", InIsolation),
                ];
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
