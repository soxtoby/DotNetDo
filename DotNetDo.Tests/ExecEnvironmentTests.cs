using System.Collections.Immutable;
using Xunit;

namespace DotNetDo.Tests;

[Collection("Process environment")]
public sealed class ExecEnvironmentTests
{
    const string InheritedName = "DOTNETDO_TEST_INHERITED_ENVIRONMENT";
    const string ChildName = "DOTNETDO_TEST_CHILD_ENVIRONMENT";

    [Fact]
    public async Task Transforms_complete_child_environment_at_execution_time()
    {
        var previous = Environment.GetEnvironmentVariable(InheritedName);
        Environment.SetEnvironmentVariable(InheritedName, "before");
        var options = new ExecOptions
            {
                Environment = environment =>
                    {
                        Assert.Equal(OperatingSystem.IsWindows(), environment.ContainsKey(InheritedName.ToLowerInvariant()));
                        return environment.Clear().Add(ChildName, environment[InheritedName]);
                    },
            };

        try
        {
            Environment.SetEnvironmentVariable(InheritedName, "at launch");

            var child = await ReadChildEnvironment(options);

            Assert.Equal("at launch", child[ChildName]);
            Assert.DoesNotContain(InheritedName, child);
        }
        finally
        {
            Environment.SetEnvironmentVariable(InheritedName, previous);
        }
    }

    [Fact]
    public async Task No_transform_preserves_inherited_environment()
    {
        var previous = Environment.GetEnvironmentVariable(InheritedName);

        try
        {
            Environment.SetEnvironmentVariable(InheritedName, "inherited");

            var child = await ReadChildEnvironment(new ExecOptions());

            Assert.Equal("inherited", child[InheritedName]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(InheritedName, previous);
        }
    }

    [Fact]
    public async Task Typed_commands_use_the_process_environment()
    {
        var command = new EnvironmentToolCommand
            {
                Environment = environment => environment.Clear().Add(ChildName, "typed"),
                Log = ExecLog.None,
            };

        var result = await command;
        var child = ParseEnvironment(result.OutputLines());

        Assert.Equal("typed", child[ChildName]);
    }

    [Fact]
    public async Task Transform_runs_for_each_launch_attempt()
    {
        var invocationCount = 0;
        var options = new ExecOptions
            {
                Environment = environment =>
                    {
                        Interlocked.Increment(ref invocationCount);
                        return environment;
                    },
            };

        await ReadChildEnvironment(options);
        await ReadChildEnvironment(options);

        Assert.Equal(2, invocationCount);
    }

    [Fact]
    public void Transform_exceptions_propagate_unchanged()
    {
        var expected = new ApplicationException("Expected test exception.");
        var options = new ExecOptions
            {
                Environment = _ => throw expected,
                Log = ExecLog.None,
            };

        var actual = Assert.Throws<ApplicationException>(() => Do.Exec(EnvironmentCommand, options));

        Assert.Same(expected, actual);
    }

    [Fact]
    public void Null_transform_results_fail_before_launch()
    {
        var options = new ExecOptions
            {
                Environment = _ => null!,
                Log = ExecLog.None,
            };

        var exception = Assert.Throws<InvalidOperationException>(() => Do.Exec(EnvironmentCommand, options));

        Assert.Equal("The process environment transformation returned null.", exception.Message);
    }

    [Fact]
    public void Null_transform_values_fail_before_launch()
    {
        var options = new ExecOptions
            {
                Environment = environment => environment.Clear().Add(ChildName, null!),
                Log = ExecLog.None,
            };

        var exception = Assert.Throws<InvalidOperationException>(() => Do.Exec(EnvironmentCommand, options));

        Assert.Equal("The process environment transformation returned a null value.", exception.Message);
    }

    [Fact]
    public async Task Environment_names_use_native_comparison()
    {
        var upperName = ChildName;
        var lowerName = ChildName.ToLowerInvariant();
        var options = new ExecOptions
            {
                Environment = _ => ImmutableDictionary<string, string>.Empty
                    .WithComparers(StringComparer.Ordinal)
                    .Add(upperName, "upper")
                    .Add(lowerName, "lower"),
                Log = ExecLog.None,
            };

        if (OperatingSystem.IsWindows())
        {
            var exception = Assert.Throws<InvalidOperationException>(() => Do.Exec(EnvironmentCommand, options));
            Assert.Contains("duplicate variable names", exception.Message);
        }
        else
        {
            var child = await ReadChildEnvironment(options);
            Assert.Equal("upper", child[upperName]);
            Assert.Equal("lower", child[lowerName]);
        }
    }

    static async Task<IReadOnlyDictionary<string, string>> ReadChildEnvironment(ExecOptions options)
    {
        var result = await Do.Exec(EnvironmentCommand, options with { Log = ExecLog.None });
        return ParseEnvironment(result.OutputLines());
    }

    static IReadOnlyDictionary<string, string> ParseEnvironment(IEnumerable<string> lines)
    {
        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var environment = new Dictionary<string, string>(comparer);

        foreach (var line in lines)
        {
            var separator = line.IndexOf('=');
            if (separator > 0)
                environment[line[..separator]] = line[(separator + 1)..];
        }

        return environment;
    }

    static string EnvironmentCommand => OperatingSystem.IsWindows()
        ? $"{(Environment.GetEnvironmentVariable("COMSPEC") ?? Path.Combine(Environment.SystemDirectory, "cmd.exe")).QuotedArgument()} /d /s /c set"
        : "/usr/bin/env";

    sealed record EnvironmentToolCommand : ExecToolCommand
    {
        protected override IReadOnlyList<string?> CommandParts => [EnvironmentCommand];
    }
}

[CollectionDefinition("Process environment", DisableParallelization = true)]
public sealed class ProcessEnvironmentCollection;
