using System.Reflection;
using System.Runtime.Versioning;
using DotNetDo.Generators;
using Xunit;

namespace DotNetDo.Tests;

public class GeneratorCompatibilityTests
{
    [Fact]
    public void Generator_targets_netstandard_2_0()
    {
        var targetFramework = typeof(MSBuildRegistrationGenerator).Assembly
            .GetCustomAttribute<TargetFrameworkAttribute>();

        Assert.Equal(".NETStandard,Version=v2.0", targetFramework?.FrameworkName);
    }
}
