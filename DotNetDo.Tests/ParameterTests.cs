using Xunit;

namespace DotNetDo.Tests;

public sealed class ParameterTests
{
    [Fact]
    public void Do_param_with_positional_string_uses_it_as_default()
    {
        Param<string> result = Do.Param(UniqueName(), "Release");

        Assert.Equal("Release", result.Value);
        Assert.Null(result.Description);
    }

    [Fact]
    public void Do_param_with_explicit_string_type_and_positional_string_uses_it_as_default()
    {
        Param<string> result = Do.Param<string>(UniqueName(), "Release");

        Assert.Equal("Release", result.Value);
        Assert.Null(result.Description);
    }

    [Fact]
    public void Do_param_with_named_description_is_optional_and_described()
    {
        OptionalParam<string> result = Do.Param(UniqueName(), description: "Package feed");

        Assert.Null(result.Value);
        Assert.Equal("Package feed", result.Description);
    }

    [Fact]
    public void Do_param_typed_with_named_description_is_optional_and_described()
    {
        OptionalParam<int> result = Do.Param<int>(UniqueName(), description: "Retry count");

        Assert.Equal("Retry count", result.Description);
    }

    static string UniqueName() => $"param-{Guid.NewGuid()}";
}
