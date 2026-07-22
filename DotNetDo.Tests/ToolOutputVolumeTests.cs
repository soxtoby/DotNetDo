using LibGit2Sharp;
using Serilog.Events;
using Xunit;

namespace DotNetDo.Tests;

[Collection("Logging level")]
public sealed class ToolOutputVolumeTests
{
    [Theory]
    [InlineData(LogEventLevel.Verbose, "diagnostic", MSBuildVerbosity.Diagnostic)]
    [InlineData(LogEventLevel.Debug, "detailed", MSBuildVerbosity.Detailed)]
    [InlineData(LogEventLevel.Information, "normal", MSBuildVerbosity.Normal)]
    [InlineData(LogEventLevel.Warning, "minimal", MSBuildVerbosity.Minimal)]
    [InlineData(LogEventLevel.Error, "quiet", MSBuildVerbosity.Quiet)]
    [InlineData(LogEventLevel.Fatal, "quiet", MSBuildVerbosity.Quiet)]
    public void Five_level_controls_map_from_logging_level(
        LogEventLevel level,
        string expected,
        MSBuildVerbosity expectedMSBuild)
    {
        AtLevel(level, () =>
            {
                ToolCommand[] commands =
                [
                    Tools.DotNet.Build,
                    Tools.DotNet.Clean,
                    Tools.DotNet.Format,
                    Tools.DotNet.Pack,
                    Tools.DotNet.Restore,
                    Tools.DotNet.Test,
                    Tools.DotNet.ToolRestore,
                    Tools.DotNet.Watch,
                ];

                Assert.All(commands, command => Assert.Contains($"--verbosity {expected}", command.ToString()));
                Assert.Equal(expected, Tools.DotNet.Build.Verbosity);
                Assert.Equal(expected, Tools.DotNet.Watch.Verbosity);

                var msbuild = Tools.MSBuild;
                Assert.Equal(expectedMSBuild, msbuild.Verbosity);
                Assert.Contains($"-verbosity:{expected}", msbuild.ToString());
            });
    }

    [Theory]
    [InlineData(LogEventLevel.Verbose, false, true)]
    [InlineData(LogEventLevel.Debug, false, true)]
    [InlineData(LogEventLevel.Information, false, false)]
    [InlineData(LogEventLevel.Warning, true, false)]
    [InlineData(LogEventLevel.Error, true, false)]
    [InlineData(LogEventLevel.Fatal, true, false)]
    public void Quiet_and_verbose_controls_map_independently(
        LogEventLevel level,
        bool expectedQuiet,
        bool expectedVerbose)
    {
        AtLevel(level, () =>
            {
                var devCerts = Tools.DotNet.DevCerts;
                var watch = Tools.DotNet.Watch;
                var push = Tools.Git.Push;
                var pushTag = Tools.Git.PushTag;

                Assert.Equal(expectedQuiet, devCerts.Quiet);
                Assert.Equal(expectedVerbose, devCerts.Verbose);
                Assert.Equal(expectedQuiet, watch.Quiet);
                Assert.Equal(expectedVerbose, watch.Verbose);
                Assert.Equal(expectedQuiet, push.Quiet);
                Assert.Equal(expectedVerbose, push.Verbose);
                Assert.Equal(expectedQuiet, pushTag.Quiet);
                Assert.Equal(expectedVerbose, pushTag.Verbose);
                Assert.Equal(expectedVerbose, Tools.Git.Add.Verbose);
                Assert.Equal(expectedQuiet, Tools.Git.Reset.Quiet);
                Assert.Equal(expectedQuiet, Tools.Git.Commit.Quiet);
            });
    }

    [Fact]
    public void Commands_snapshot_logging_level_when_created()
    {
        var original = Logging.Level;
        try
        {
            Logging.Level = LogEventLevel.Debug;
            var watch = Tools.DotNet.Watch;
            var push = Tools.Git.Push;

            Logging.Level = LogEventLevel.Error;

            Assert.Equal("detailed", watch.Verbosity);
            Assert.True(watch.Verbose);
            Assert.False(watch.Quiet);
            Assert.True(push.Verbose);
            Assert.False(push.Quiet);
            Assert.Equal("quiet", Tools.DotNet.Watch.Verbosity);
            Assert.True(Tools.Git.Push.Quiet);
        }
        finally
        {
            Logging.Level = original;
        }
    }

    [Fact]
    public void Explicit_values_override_only_their_control()
    {
        AtLevel(LogEventLevel.Warning, () =>
            {
                var watch = Tools.DotNet.Watch with { Quiet = false };
                Assert.DoesNotContain("--quiet", watch.ToString());
                Assert.Contains("--verbosity minimal", watch.ToString());

                var nativeVerbosity = Tools.DotNet.Watch with { Verbosity = null };
                Assert.Contains("--quiet", nativeVerbosity.ToString());
                Assert.DoesNotContain("--verbosity", nativeVerbosity.ToString());

                var push = Tools.Git.Push with { Quiet = false };
                Assert.False(push.Quiet);
            });
    }

    [Fact]
    public void Git_volume_controls_render_before_positional_arguments()
    {
        var directory = Path.Combine(Do.WorkingDirectory, ".test-workspaces", $"volume-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        Repository.Init(directory);

        try
        {
            AtLevel(LogEventLevel.Debug, () =>
                {
                    using var git = new GitRepository(AbsolutePath.Parse(directory));
                    Assert.Contains(" add --verbose --all", (git.Add with { All = true }).ToString());
                });

            AtLevel(LogEventLevel.Warning, () =>
                {
                    using var git = new GitRepository(AbsolutePath.Parse(directory));
                    Assert.Contains(" reset --quiet -- .", (git.Reset with { All = true }).ToString());
                    Assert.Contains(" commit --quiet --message test", (git.Commit with { Message = "test" }).ToString());
                    Assert.EndsWith(" push --quiet", git.Push.ToString());
                    Assert.DoesNotContain("--quiet", (git.Push with { Quiet = false }).ToString());
                });
        }
        finally
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Additional_arguments_remain_opaque()
    {
        AtLevel(LogEventLevel.Information, () =>
            {
                var rendered = (Tools.DotNet.Build with { AdditionalArguments = "--verbosity minimal" }).ToString();
                Assert.Contains("--verbosity normal", rendered);
                Assert.EndsWith("--verbosity minimal", rendered);
            });
    }

    static void AtLevel(LogEventLevel level, Action action)
    {
        var original = Logging.Level;
        try
        {
            Logging.Level = level;
            action();
        }
        finally
        {
            Logging.Level = original;
        }
    }
}

[CollectionDefinition("Logging level", DisableParallelization = true)]
public sealed class LoggingLevelCollection { }
