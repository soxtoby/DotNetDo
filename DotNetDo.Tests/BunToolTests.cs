using Serilog.Events;
using Xunit;

namespace DotNetDo.Tests;

[Collection("Logging level")]
public sealed class BunToolTests
{
    [Fact]
    public void Install_renders_packages_and_options_in_canonical_order()
    {
        var command = Tools.Bun.Install with
        {
            Packages = ["first package"],
            Production = true,
            FrozenLockfile = true,
            Omit = [BunDependencyType.Dev, BunDependencyType.Peer],
            Linker = BunLinker.Isolated,
            Filters = ["web app"],
        };

        Assert.Equal(
            "bun install \"first package\" --production --frozen-lockfile --omit dev --omit peer --linker isolated --filter \"web app\"",
            command.ToString());
    }

    [Fact]
    public void Add_requires_packages_and_validates_dependency_kind()
    {
        Assert.Throws<InvalidOperationException>(() => Tools.Bun.Add.ToString());
        Assert.Throws<InvalidOperationException>(() =>
            (Tools.Bun.Add with { Packages = ["zod"], Dev = true, Peer = true }).ToString());
        Assert.Equal(
            "bun add \"my package\" --dev --exact",
            (Tools.Bun.Add with { Packages = ["my package"], Dev = true, Exact = true }).ToString());
    }

    [Fact]
    public void Run_requires_target_and_quotes_forwarded_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => Tools.Bun.Run.ToString());
        Assert.Equal(
            "bun run \"build app\" --silent --filter \"web app\" --bun --if-present --output \"dist folder\"",
            (Tools.Bun.Run with
            {
                Target = "build app",
                Silent = true,
                Filters = ["web app"],
                UseBun = true,
                IfPresent = true,
                Arguments = ["--output", "dist folder"],
            }).ToString());
    }

    [Fact]
    public void Test_renders_patterns_and_options()
    {
        Assert.Equal(
            "bun test \"unit tests\" --timeout 10000 --update-snapshots --coverage --bail 2 --test-name-pattern \"does work\" --pass-with-no-tests",
            (Tools.Bun.Test with
            {
                Patterns = ["unit tests"],
                Timeout = 10000,
                UpdateSnapshots = true,
                Coverage = true,
                Bail = 2,
                TestNamePattern = "does work",
                PassWithNoTests = true,
            }).ToString());
    }

    [Fact]
    public void Build_requires_entries_and_validates_output()
    {
        Assert.Throws<InvalidOperationException>(() => Tools.Bun.Build.ToString());
        Assert.Throws<InvalidOperationException>(() =>
            (Tools.Bun.Build with { EntryPoints = ["src/index.ts"], OutDir = "dist", OutFile = "app.js" }).ToString());
        Assert.Equal(
            "bun build \"src/main app.ts\" --production --target browser --outdir \"dist folder\" --splitting --external react --external \"react dom\" --format esm --minify",
            (Tools.Bun.Build with
            {
                EntryPoints = ["src/main app.ts"],
                Production = true,
                Target = BunBuildTarget.Browser,
                OutDir = "dist folder",
                Splitting = true,
                External = ["react", "react dom"],
                Format = BunBuildFormat.Esm,
                Minify = true,
            }).ToString());
    }

    [Fact]
    public void Publish_renders_package_and_registry_options()
    {
        Assert.Equal(
            "bun publish \"package archive.tgz\" --dry-run --access public --tag \"next release\" --otp \"123 456\" --tolerate-republish",
            (Tools.Bun.Publish with
            {
                Package = "package archive.tgz",
                DryRun = true,
                Access = BunAccess.Public,
                Tag = "next release",
                Otp = "123 456",
                TolerateRepublish = true,
            }).ToString());
    }

    [Fact]
    public void Getters_are_fresh_and_with_replaces_snapshots()
    {
        Assert.NotSame(Tools.Bun.Install, Tools.Bun.Install);
        var original = Tools.Bun.Install with { Packages = ["one"] };
        var replacement = original with { Packages = ["two words"] };
        Assert.Equal("bun install \"two words\"", replacement.ToString());
    }

    [Fact]
    public void Output_volume_snapshots_logging_level_and_can_be_cleared()
    {
        var original = Logging.Level;
        try
        {
            Logging.Level = LogEventLevel.Debug;
            var verbose = Tools.Bun.Install;
            Logging.Level = LogEventLevel.Warning;

            Assert.True(verbose.Verbose);
            Assert.Equal("bun install --verbose", verbose.ToString());
            Assert.Equal("bun install --quiet", Tools.Bun.Install.ToString());
            Assert.Equal("bun install", (Tools.Bun.Install with { Quiet = false }).ToString());
        }
        finally
        {
            Logging.Level = original;
        }
    }
}
