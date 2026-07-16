using System.Text.Json;
using YamlDotNet.Core;
using Xunit;

namespace DotNetDo.Tests;

public sealed class ExecResultTests
{
    [Fact]
    public void Reads_individual_streams_and_reconstructed_output()
    {
        var result = Result(
            new ExecOutput(OutputType.Out, "one"),
            new ExecOutput(OutputType.Error, "problem"),
            new ExecOutput(OutputType.Out, "two"));

        var output = result.OutputLines();

        Assert.Equal(["one", "two"], output);
        Assert.Equal(["problem"], result.ErrorLines());
        Assert.Equal($"one{Environment.NewLine}two", result.ReadText());
        output[0] = "changed";
        Assert.Equal("one", result.OutputLines()[0]);
    }

    [Fact]
    public void Reads_structured_standard_output()
    {
        Assert.Equal("json", Result(new ExecOutput(OutputType.Out, "{\"Value\":\"json\"}"))
            .ReadJson<Content>()!.Value);
        Assert.Equal("toml", Result(new ExecOutput(OutputType.Out, "Value = \"toml\""))
            .ReadToml<Content>()!.Value);
        Assert.Equal("yaml", Result(new ExecOutput(OutputType.Out, "Value: yaml"))
            .ReadYaml<Content>()!.Value);
        Assert.Equal("xml", Result(new ExecOutput(OutputType.Out, "<Content><Value>xml</Value></Content>"))
            .ReadXml<Content>()!.Value);
    }

    [Fact]
    public void Structured_readers_expose_serializer_failures()
    {
        Assert.Throws<JsonException>(() => Result(new ExecOutput(OutputType.Out, "not json")).ReadJson<Content>());
        Assert.Throws<YamlException>(() => Result(new ExecOutput(OutputType.Out, "[not yaml")).ReadYaml<Content>());
    }

    static ExecResult Result(params ExecOutput[] output) => new()
    {
        Command = "example",
        WorkingDirectory = "work",
        ExitCode = 1,
        AllOutput = output,
    };

    public sealed class Content
    {
        public string? Value { get; set; }
    }
}
