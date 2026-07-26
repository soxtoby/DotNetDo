using Xunit;

namespace DotNetDo.Tests;

public sealed class AzureToolTests
{
    static readonly AbsolutePath File = AbsolutePath.Parse(Path.Combine(Path.GetTempPath(), "Bicep files", "main.bicep"));
    static readonly AbsolutePath ParamsFile = AbsolutePath.Parse(Path.Combine(Path.GetTempPath(), "Bicep files", "main.bicepparam"));

    [Fact]
    public void Bicep_commands_render_their_supported_verbs()
    {
        AzureBicepCommand[] commands =
            [
                Tools.Azure.Bicep.Build with { File = File },
                Tools.Azure.Bicep.BuildParams with { File = ParamsFile },
                Tools.Azure.Bicep.Lint with { File = File },
                Tools.Azure.Bicep.Format with { File = File },
                Tools.Azure.Bicep.GenerateParams with { File = File },
                Tools.Azure.Bicep.Restore with { File = File },
                Tools.Azure.Bicep.Publish with { File = File, Target = "br:registry/modules/example:v1" },
                Tools.Azure.Bicep.Snapshot with { File = ParamsFile },
                Tools.Azure.Bicep.Install,
                Tools.Azure.Bicep.Upgrade,
            ];

        var verbs = new[]
            {
                "build",
                "build-params",
                "lint",
                "format",
                "generate-params",
                "restore",
                "publish",
                "snapshot",
                "install",
                "upgrade",
            };

        for (var index = 0; index < commands.Length; index++)
            Assert.StartsWith($"az bicep {verbs[index]}", commands[index].ToString());
    }

    [Fact]
    public void Build_renders_typed_paths_and_global_options_in_canonical_order()
    {
        var output = AbsolutePath.Parse(Path.Combine(Path.GetTempPath(), "ARM output", "azuredeploy.json"));
        var command = Tools.Azure.Bicep.Build with
        {
            File = File,
            NoRestore = true,
            OutFile = output,
            Debug = false,
            Verbose = true,
            OnlyShowErrors = false,
            Subscription = "Production Subscription",
            Output = AzureOutputFormat.JsonC,
            Query = "properties.outputs",
            AdditionalArguments = "--future value",
        };

        Assert.Equal(
            $"az bicep build --file {File.QuotedArgument()} --no-restore --outfile {output.QuotedArgument()} --verbose --subscription \"Production Subscription\" --output jsonc --query properties.outputs --future value",
            command.ToString());
    }

    [Fact]
    public void Bicep_specific_tokens_render_exactly()
    {
        var formatted = Tools.Azure.Bicep.Format with
        {
            File = File,
            IndentKind = BicepIndentKind.Space,
            IndentSize = 2,
            InsertFinalNewline = true,
            NewlineKind = BicepNewlineKind.CRLF,
            Stdout = true,
        };
        Assert.Contains("--indent-kind Space --indent-size 2 --insert-final-newline --newline-kind CRLF --stdout", formatted.ToString());

        var generated = Tools.Azure.Bicep.GenerateParams with
        {
            File = File,
            IncludeParameters = BicepIncludedParameters.RequiredOnly,
            OutputFormat = BicepParameterOutputFormat.BicepParam,
        };
        Assert.Contains("--include-params RequiredOnly --output-format bicepparam", generated.ToString());

        var installed = Tools.Azure.Bicep.Install with
        {
            TargetPlatform = BicepTargetPlatform.LinuxMuslX64,
            Version = "v0.42.1-preview.1",
        };
        Assert.StartsWith("az bicep install --target-platform linux-musl-x64 --version v0.42.1-preview.1", installed.ToString());
        Assert.StartsWith(
            "az bicep upgrade --target-platform win-arm64",
            (Tools.Azure.Bicep.Upgrade with { TargetPlatform = BicepTargetPlatform.WinArm64 }).ToString());
    }

    [Fact]
    public void Snapshot_renders_semantic_identifiers()
    {
        var subscription = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tenant = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var command = Tools.Azure.Bicep.Snapshot with
        {
            File = ParamsFile,
            Mode = BicepSnapshotMode.Validate,
            ResourceGroup = "production resources",
            SubscriptionId = subscription,
            TenantId = tenant,
        };

        Assert.Contains(
            $"--mode Validate --resource-group \"production resources\" --subscription-id {subscription} --tenant-id {tenant}",
            command.ToString());
    }

    [Fact]
    public void Required_values_and_structural_conflicts_fail_during_rendering()
    {
        Assert.Throws<ArgumentNullException>(() => Tools.Azure.Bicep.Build.ToString());
        Assert.Throws<ArgumentNullException>(() => (Tools.Azure.Bicep.Publish with { File = File }).ToString());
        Assert.Throws<InvalidOperationException>(() => (Tools.Azure.Bicep.Build with
        {
            File = File,
            OutDirectory = File.Parent,
            Stdout = true,
        }).ToString());
        Assert.Throws<InvalidOperationException>(() => (Tools.Azure.Bicep.Format with
        {
            File = File,
            IndentKind = BicepIndentKind.Tab,
            IndentSize = 2,
        }).ToString());
    }

    [Fact]
    public void Defaults_are_fresh()
    {
        Assert.NotSame(Tools.Azure.Bicep.Build, Tools.Azure.Bicep.Build);
        Assert.NotSame(Tools.Azure.Bicep.Upgrade, Tools.Azure.Bicep.Upgrade);
    }
}
