using System.Text.Json;
using System.Xml.Linq;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
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
    public void Reads_document_models_from_standard_output()
    {
        Assert.Equal("json", Result(new ExecOutput(OutputType.Out, "{\"Value\":\"json\"}"))
            .ReadJson()!["Value"]!.GetValue<string>());
        Assert.Equal("toml", Result(new ExecOutput(OutputType.Out, "Value = \"toml\""))
            .ReadToml()["Value"]);
        Assert.Equal("yaml", ((YamlMappingNode)Result(new ExecOutput(OutputType.Out, "Value: yaml"))
            .ReadYaml()!).Children[new YamlScalarNode("Value")].ToString());
        Assert.Equal("xml", Result(new ExecOutput(OutputType.Out, "<Content><Value>xml</Value></Content>"))
            .ReadXml().Root!.Element("Value")!.Value);
    }

    [Fact]
    public void Yaml_document_model_reader_requires_at_most_one_document()
    {
        Assert.Null(Result().ReadYaml());
        Assert.Throws<YamlException>(() => Result(new ExecOutput(OutputType.Out, "one\n---\ntwo")).ReadYaml());
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
