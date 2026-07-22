using Xunit;

namespace DotNetDo.Tests;

public sealed class ScoopToolTests
{
    [Fact]
    public void Install_renders_apps_and_options_in_canonical_order()
    {
        var command = Tools.Scoop.Install with
            {
                Apps = ["azure-cli", "7zip"],
                Global = true,
                Independent = true,
                NoCache = true,
                NoUpdateScoop = true,
                SkipHashCheck = true,
                Architecture = "64bit",
            };

        Assert.Equal(
            "scoop install azure-cli 7zip --global --independent --no-cache --no-update-scoop --skip-hash-check --arch 64bit",
            command.ToString());
    }

    [Fact]
    public void Install_requires_at_least_one_app()
    {
        Assert.Throws<InvalidOperationException>(() => Tools.Scoop.Install.ToString());
    }

    [Fact]
    public void Uninstall_renders_apps_and_options()
    {
        var command = Tools.Scoop.Uninstall with { Apps = ["azure-cli"], Global = true, Purge = true };

        Assert.Equal("scoop uninstall azure-cli --global --purge", command.ToString());
    }

    [Fact]
    public void Uninstall_requires_at_least_one_app()
    {
        Assert.Throws<InvalidOperationException>(() => Tools.Scoop.Uninstall.ToString());
    }

    [Fact]
    public void Update_without_apps_updates_scoop_itself()
    {
        Assert.Equal("scoop update", Tools.Scoop.Update.ToString());
    }

    [Fact]
    public void Update_renders_apps_and_options()
    {
        var command = Tools.Scoop.Update with { Apps = ["azure-cli"], Force = true, Quiet = true };

        Assert.Equal("scoop update azure-cli --force --quiet", command.ToString());
    }

    [Fact]
    public void Update_all_renders_all_switch()
    {
        Assert.Equal("scoop update --all", (Tools.Scoop.Update with { All = true }).ToString());
    }

    [Fact]
    public void Update_rejects_apps_combined_with_all()
    {
        var command = Tools.Scoop.Update with { Apps = ["azure-cli"], All = true };

        Assert.Throws<InvalidOperationException>(() => command.ToString());
    }

    [Theory]
    [InlineData(ScoopBucketCommand.Add, "extras", null, "scoop bucket add extras")]
    [InlineData(ScoopBucketCommand.Add, "custom", "https://example.com/bucket.git", "scoop bucket add custom https://example.com/bucket.git")]
    [InlineData(ScoopBucketCommand.Rm, "extras", null, "scoop bucket rm extras")]
    [InlineData(ScoopBucketCommand.List, null, null, "scoop bucket list")]
    [InlineData(ScoopBucketCommand.Known, null, null, "scoop bucket known")]
    public void Bucket_renders_subcommand_name_and_repository(ScoopBucketCommand subcommand, string? name, string? repository, string expected)
    {
        var command = Tools.Scoop.Bucket with { Command = subcommand, Name = name, Repository = repository };

        Assert.Equal(expected, command.ToString());
    }

    [Fact]
    public void Bucket_custom_command_controls_rendering()
    {
        var command = Tools.Scoop.Bucket with { Command = ScoopBucketCommand.Add, CustomCommand = "future", Name = "extras" };

        Assert.Equal("scoop bucket future extras", command.ToString());
    }

    [Fact]
    public void Bucket_requires_a_subcommand()
    {
        Assert.Throws<InvalidOperationException>(() => Tools.Scoop.Bucket.ToString());
    }

    [Theory]
    [InlineData(null, null, false, "scoop config")]
    [InlineData("aria2-enabled", null, false, "scoop config aria2-enabled")]
    [InlineData("aria2-enabled", "false", false, "scoop config aria2-enabled false")]
    [InlineData("aria2-enabled", null, true, "scoop config rm aria2-enabled")]
    public void Config_renders_show_get_set_and_remove(string? name, string? value, bool remove, string expected)
    {
        var command = Tools.Scoop.Config with { Name = name, Value = value, Remove = remove };

        Assert.Equal(expected, command.ToString());
    }

    [Fact]
    public void Config_remove_requires_name()
    {
        Assert.Throws<InvalidOperationException>(() => (Tools.Scoop.Config with { Remove = true }).ToString());
    }

    [Fact]
    public void Config_value_requires_name()
    {
        Assert.Throws<InvalidOperationException>(() => (Tools.Scoop.Config with { Value = "false" }).ToString());
    }

    [Fact]
    public void Config_rejects_value_combined_with_remove()
    {
        var command = Tools.Scoop.Config with { Name = "aria2-enabled", Value = "false", Remove = true };

        Assert.Throws<InvalidOperationException>(() => command.ToString());
    }
}
