using Xunit;

namespace DotNetDo.Tests;

public sealed class DotNetToolTests
{
    [Fact]
    public async Task Uses_Do_Solution_as_the_default_target()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"dotnetdo-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "Product.slnx");
        await File.WriteAllTextAsync(path, "<Solution />", TestContext.Current.CancellationToken);
        var original = Do.Solution;

        try
        {
            Do.Solution = await Solution.Load(path, TestContext.Current.CancellationToken);

            Assert.Equal($"dotnet build {path.QuotedArgument()}", Tools.DotNet.Build.ToString());
            Assert.Equal("dotnet test App.csproj", (Tools.DotNet.Test with { Targets = ["App.csproj"] }).ToString());
        }
        finally
        {
            Do.Solution = original;
            Directory.Delete(directory, true);
        }
    }
}
