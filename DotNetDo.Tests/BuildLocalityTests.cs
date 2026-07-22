using Xunit;

namespace DotNetDo.Tests;

public sealed class BuildLocalityTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    public void CI_boole_accept_empty_and_strict_boolean_values(string? value, bool? expected)
    {
        const string name = "DOTNETDO_TEST_CI_BOOLEAN";
        var previous = Environment.GetEnvironmentVariable(name);
        try
        {
            Environment.SetEnvironmentVariable(name, value);
            Assert.Equal(expected, CIEnvironment.Bool(name));
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, previous);
        }
    }

    [Fact]
    public void Invalid_CI_boole_fail()
    {
        const string name = "DOTNETDO_TEST_INVALID_CI_BOOLEAN";
        var previous = Environment.GetEnvironmentVariable(name);
        try
        {
            Environment.SetEnvironmentVariable(name, "sometimes");
            Assert.Throws<FormatException>(() => CIEnvironment.Bool(name));
        }
        finally
        {
            Environment.SetEnvironmentVariable(name, previous);
        }
    }

    [Fact]
    public void Tool_configuration_defaults_follow_build_locality()
    {
        var expected = Do.IsLocalBuild ? null : "Release";

        Assert.Equal(expected, Tools.DotNet.Build.Configuration);
        Assert.Equal(expected, Tools.DotNet.Clean.Configuration);
        Assert.Equal(expected, Tools.DotNet.Pack.Configuration);
        Assert.Equal(expected, Tools.DotNet.Test.Configuration);
        Assert.Equal(expected, Tools.DotNet.Watch.Configuration);

        var properties = Tools.MSBuild.Properties;
        if (expected is null)
            Assert.DoesNotContain("Configuration", properties);
        else
            Assert.Equal(expected, properties["Configuration"]);
    }
}
