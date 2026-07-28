using DotNetDo.Cli;
using Xunit;

namespace DotNetDo.Tests;

public sealed class TaskHelpTests
{
    [Fact]
    public void Named_default_value_is_discovered_without_its_argument_name()
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, """var name = Do.Param("name", defaultValue: "foo");""");

            var parameter = Assert.Single(TaskHelp.Discover(file));

            Assert.Equal("foo", parameter.DefaultValue);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Theory]
    [InlineData("""var name = Do.Param("name", defaultValue: "foo", description: "The name");""")]
    public void Named_description_is_discovered_without_its_argument_name(string source)
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, source);

            var parameter = Assert.Single(TaskHelp.Discover(file));

            Assert.Equal("The name", parameter.Description);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
