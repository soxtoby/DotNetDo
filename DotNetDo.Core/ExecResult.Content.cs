using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using System.Xml.Serialization;
using Tomlyn;
using Tomlyn.Model;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace DotNetDo;

public sealed partial record ExecResult
{
    /// <summary>Returns the captured standard-output messages in observed order.</summary>
    public string[] OutputLines() => AllOutput
        .Where(output => output.Type == OutputType.Out)
        .Select(output => output.Message)
        .ToArray();

    /// <summary>Returns the captured standard-error messages in observed order.</summary>
    public string[] ErrorLines() => AllOutput
        .Where(output => output.Type == OutputType.Error)
        .Select(output => output.Message)
        .ToArray();

    /// <summary>Joins the captured standard-output lines using the current environment's newline.</summary>
    public string ReadText() => string.Join(Environment.NewLine, OutputLines());

    /// <summary>Deserializes the captured standard output into the requested value type.</summary>
    public T? ReadJson<T>(JsonSerializerOptions? options = null) =>
        JsonSerializer.Deserialize<T>(ReadText(), options);

    /// <summary>Reads the captured standard output as a JSON document model.</summary>
    public JsonNode? ReadJson(JsonSerializerOptions? options = null) => ReadJson<JsonNode>(options);

    /// <summary>Deserializes the captured standard output into the requested value type.</summary>
    public T? ReadToml<T>(TomlSerializerOptions? options = null) =>
        TomlSerializer.Deserialize<T>(ReadText(), options);

    /// <summary>Reads the captured standard output as a TOML document model.</summary>
    public TomlTable ReadToml(TomlSerializerOptions? options = null) => ReadToml<TomlTable>(options)!;

    /// <summary>Deserializes one YAML document from the captured standard output.</summary>
    public T? ReadYaml<T>(IDeserializer? deserializer = null) =>
        (deserializer ?? YamlSerialization.Deserializer).Deserialize<T>(ReadText());

    /// <summary>Reads the root node of one YAML document from the captured standard output.</summary>
    public YamlNode? ReadYaml()
    {
        using var reader = new StringReader(ReadText());
        return YamlSerialization.ReadNode(reader);
    }

    /// <summary>Deserializes the captured standard output into the requested value type.</summary>
    public T? ReadXml<T>()
    {
        using var reader = new StringReader(ReadText());
        return (T?)new XmlSerializer(typeof(T)).Deserialize(reader);
    }

    /// <summary>Reads the captured standard output as an XML document model.</summary>
    public XDocument ReadXml()
    {
        using var reader = new StringReader(ReadText());
        return XDocument.Load(reader);
    }
}
