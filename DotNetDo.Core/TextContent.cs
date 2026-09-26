using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using System.Xml.Serialization;
using Tomlyn;
using Tomlyn.Model;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace DotNetDo;

/// <summary>Structured readers shared by results whose content is already decoded text.</summary>
static class TextContent
{
    public static T? ReadJson<T>(string text, JsonSerializerOptions? options) =>
        JsonSerializer.Deserialize<T>(text, options);

    public static JsonNode? ReadJson(string text, JsonSerializerOptions? options) => ReadJson<JsonNode>(text, options);

    public static T? ReadToml<T>(string text, TomlSerializerOptions? options) =>
        TomlSerializer.Deserialize<T>(text, options);

    public static TomlTable ReadToml(string text, TomlSerializerOptions? options) => ReadToml<TomlTable>(text, options)!;

    public static T? ReadYaml<T>(string text, IDeserializer? deserializer) =>
        (deserializer ?? YamlSerialization.Deserializer).Deserialize<T>(text);

    public static YamlNode? ReadYaml(string text)
    {
        using var reader = new StringReader(text);
        return YamlSerialization.ReadNode(reader);
    }

    public static T? ReadXml<T>(string text)
    {
        using var reader = new StringReader(text);
        return (T?)new XmlSerializer(typeof(T)).Deserialize(reader);
    }

    public static XDocument ReadXml(string text)
    {
        using var reader = new StringReader(text);
        return XDocument.Load(reader);
    }
}
