using Xunit;

namespace DotNetDo.Tests;

public sealed class NpmToolTests
{
    [Fact]
    public void Install_renders_packages_and_options_in_canonical_order()
    {
        var command = Tools.Npm.Install with
        {
            Packages = ["first package", "second"],
            SaveDev = true,
            SaveExact = true,
            InstallStrategy = NpmInstallStrategy.Linked,
            Omit = [NpmDependencyType.Optional, NpmDependencyType.Peer],
            Audit = false,
            Workspaces = ["web app"],
        };

        Assert.Equal(
            "npm install \"first package\" second --save-dev --save-exact --install-strategy linked --omit optional peer --no-audit --workspace \"web app\"",
            command.ToString());
    }

    [Fact]
    public void Ci_renders_shared_install_options()
    {
        var command = Tools.Npm.CleanInstall with
        {
            Include = [NpmDependencyType.Dev],
            StrictPeerDependencies = true,
            IgnoreScripts = true,
            Fund = false,
            DryRun = true,
        };

        Assert.Equal("npm ci --include dev --strict-peer-deps --ignore-scripts --no-fund --dry-run", command.ToString());
    }

    [Fact]
    public void Run_requires_script_and_forwards_quoted_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => Tools.Npm.Run.ToString());

        var command = Tools.Npm.Run with
        {
            Script = "build app",
            IfPresent = true,
            ScriptShell = "Power Shell",
            Arguments = ["--output", "dist folder"],
        };

        Assert.Equal(
            "npm run \"build app\" --if-present --script-shell \"Power Shell\" -- --output \"dist folder\"",
            command.ToString());
    }

    [Fact]
    public void Test_forwards_arguments_after_separator()
    {
        Assert.Equal(
            "npm test --ignore-scripts -- --filter \"unit tests\"",
            (Tools.Npm.Test with { IgnoreScripts = true, Arguments = ["--filter", "unit tests"] }).ToString());
    }

    [Fact]
    public void Pack_renders_destination_workspace_and_flags()
    {
        var command = Tools.Npm.Pack with
        {
            Package = "./my package",
            DryRun = true,
            Json = true,
            Destination = "pack output",
            AllWorkspaces = true,
            IncludeWorkspaceRoot = true,
        };

        Assert.Equal(
            "npm pack \"./my package\" --dry-run --json --pack-destination \"pack output\" --workspaces --include-workspace-root",
            command.ToString());
    }

    [Fact]
    public void Publish_renders_options_and_validates_provenance()
    {
        var command = Tools.Npm.Publish with
        {
            Package = "package.tgz",
            Tag = "next release",
            Access = NpmAccess.Public,
            DryRun = true,
            Otp = "123 456",
            ProvenanceFile = "provenance file.json",
        };

        Assert.Equal(
            "npm publish package.tgz --tag \"next release\" --access public --dry-run --otp \"123 456\" --provenance-file \"provenance file.json\"",
            command.ToString());
        Assert.Throws<InvalidOperationException>(() =>
            (Tools.Npm.Publish with { Provenance = true, ProvenanceFile = "statement.json" }).ToString());
    }

    [Fact]
    public void Workspace_selection_is_exclusive()
    {
        Assert.Throws<InvalidOperationException>(() =>
            (Tools.Npm.CleanInstall with { Workspaces = ["app"], AllWorkspaces = true }).ToString());
    }

    [Fact]
    public void Getters_are_fresh_and_with_replaces_values()
    {
        Assert.NotSame(Tools.Npm.Install, Tools.Npm.Install);
        var original = Tools.Npm.Install with { Packages = ["one"] };
        var replacement = original with { Packages = ["two words"] };
        Assert.Equal("npm install \"two words\"", replacement.ToString());
    }
}
