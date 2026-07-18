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
            var command = Tools.DotNet.Test with { Targets = ["My App.csproj"], Output = "test output" };
            Assert.Equal(["My App.csproj"], command.Targets);
            Assert.Equal("test output", command.Output);
            Assert.Equal("dotnet test \"My App.csproj\" --output \"test output\"", command.ToString());
        }
        finally
        {
            Do.Solution = original;
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void Tool_commands_quote_semantic_values_during_rendering()
    {
        var values = new List<string> { "one value", "" };
        var command = new TestToolCommand
        {
            Value = "scalar value",
            Values = values,
            Raw = "--first second",
            Blank = "   ",
        };
        values[0] = "changed";

        Assert.Equal("scalar value", command.Value);
        Assert.Equal(["one value", ""], command.Values);
        Assert.Equal("--first second", command.Raw);
        Assert.Equal("   ", command.Blank);
        Assert.Equal("example --value \"scalar value\" --values \"one value\" --raw --first second", command.ToString());

        var replacement = command with { Blank = "later value" };
        Assert.Equal("example --value \"scalar value\" --values \"one value\" --raw --first second --blank \"later value\"", replacement.ToString());

        var preQuoted = command with { Value = "scalar value".QuotedArgument() };
        Assert.Equal("example --value \"\\\"scalar value\\\"\" --values \"one value\" --raw --first second", preQuoted.ToString());
    }

    sealed record TestToolCommand : ExecToolCommand
    {
        protected override string CommandPrefix => "example";
        public string? Value { get => GetArgument("value"); init => SetArgument("value", "--value ", value); }
        public IReadOnlyList<string> Values { get => GetArgumentArray("values"); init => SetArgumentArray("values", "--values ", value); }
        public string? Raw { get => GetArgument("raw"); init => SetArgument("raw", "--raw ", value, quote: false); }
        public string? Blank { get => GetArgument("blank"); init => SetArgument("blank", "--blank ", value); }
    }
}
