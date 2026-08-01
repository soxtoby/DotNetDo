using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using System.Xml.Serialization;
using Tomlyn;
using Tomlyn.Model;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace DotNetDo;

public sealed partial record AbsolutePath
{
    /// <summary>Reads the entire file as text using the supplied encoding or UTF-8.</summary>
    /// <param name="encoding">The text encoding; <see langword="null"/> uses UTF-8.</param>
    public string ReadText(Encoding? encoding = null) =>
        encoding is null ? File.ReadAllText(this) : File.ReadAllText(this, encoding);

    /// <summary>Reads all file lines using the supplied encoding or UTF-8.</summary>
    /// <param name="encoding">The text encoding; <see langword="null"/> uses UTF-8.</param>
    public string[] ReadLines(Encoding? encoding = null) =>
        encoding is null ? File.ReadAllLines(this) : File.ReadAllLines(this, encoding);

    /// <summary>Writes text to this existing file location using the supplied encoding or UTF-8.</summary>
    /// <param name="text">The complete file content. Missing parent directories are not created.</param>
    /// <param name="encoding">The text encoding; <see langword="null"/> uses UTF-8.</param>
    public void WriteText(string text, Encoding? encoding = null)
    {
        if (encoding is null)
            File.WriteAllText(this, text);
        else
            File.WriteAllText(this, text, encoding);
    }

    /// <summary>Writes lines to this existing file location using the supplied encoding or UTF-8.</summary>
    /// <param name="lines">The lines to write using the platform newline. Missing parent directories are not created.</param>
    /// <param name="encoding">The text encoding; <see langword="null"/> uses UTF-8.</param>
    public void WriteLines(IEnumerable<string> lines, Encoding? encoding = null)
    {
        if (encoding is null)
            File.WriteAllLines(this, lines);
        else
            File.WriteAllLines(this, lines, encoding);
    }

    /// <summary>Deserializes the file into the requested value type.</summary>
    /// <param name="options">JSON serializer behavior; <see langword="null"/> uses <see cref="JsonSerializerOptions.Default"/>.</param>
    public T? ReadJson<T>(JsonSerializerOptions? options = null)
    {
        using var stream = File.OpenRead(this);
        return JsonSerializer.Deserialize<T>(stream, options);
    }

    /// <summary>Reads the file as a JSON document model.</summary>
    public JsonNode? ReadJson(JsonSerializerOptions? options = null) => ReadJson<JsonNode>(options);

    /// <summary>Serializes the value to this file.</summary>
    /// <param name="value">The value serialized as JSON. Missing parent directories are not created.</param>
    /// <param name="options">JSON serializer behavior; <see langword="null"/> uses <see cref="JsonSerializerOptions.Default"/>.</param>
    public void WriteJson<T>(T value, JsonSerializerOptions? options = null)
    {
        using var stream = File.Create(this);
        JsonSerializer.Serialize(stream, value, options);
    }

    /// <summary>Deserializes the file into the requested value type.</summary>
    /// <param name="options">TOML serializer behavior; <see langword="null"/> uses Tomlyn defaults.</param>
    public T? ReadToml<T>(TomlSerializerOptions? options = null)
    {
        using var stream = File.OpenRead(this);
        return TomlSerializer.Deserialize<T>(stream, options);
    }

    /// <summary>Reads the file as a TOML document model.</summary>
    public TomlTable ReadToml(TomlSerializerOptions? options = null) => ReadToml<TomlTable>(options)!;

    /// <summary>Serializes the value to this file.</summary>
    /// <param name="value">The value serialized as TOML. Missing parent directories are not created.</param>
    /// <param name="options">TOML serializer behavior; <see langword="null"/> uses Tomlyn defaults.</param>
    public void WriteToml<T>(T value, TomlSerializerOptions? options = null)
    {
        using var stream = File.Create(this);
        TomlSerializer.Serialize(stream, value, options);
    }

    /// <summary>Deserializes one YAML document from the file into the requested value type.</summary>
    /// <param name="deserializer">The YAML deserializer; <see langword="null"/> uses DotNetDo's default instance.</param>
    public T? ReadYaml<T>(IDeserializer? deserializer = null)
    {
        using var reader = File.OpenText(this);
        return (deserializer ?? YamlSerialization.Deserializer).Deserialize<T>(reader);
    }

    /// <summary>Reads the root node of one YAML document from the file.</summary>
    public YamlNode? ReadYaml()
    {
        using var reader = File.OpenText(this);
        return YamlSerialization.ReadNode(reader);
    }

    /// <summary>Serializes the value as one YAML document to this file.</summary>
    /// <param name="value">The value serialized as YAML. Missing parent directories are not created.</param>
    /// <param name="serializer">The YAML serializer; <see langword="null"/> uses DotNetDo's default instance.</param>
    public void WriteYaml<T>(T value, ISerializer? serializer = null)
    {
        using var writer = File.CreateText(this);
        (serializer ?? YamlSerialization.Serializer).Serialize(writer, value);
    }

    /// <summary>Writes one YAML document-model root node to this file.</summary>
    public void WriteYaml(YamlNode value)
    {
        using var writer = File.CreateText(this);
        new YamlStream(new YamlDocument(value)).Save(writer);
    }

    /// <summary>Deserializes the file into the requested value type.</summary>
    public T? ReadXml<T>()
    {
        using var stream = File.OpenRead(this);
        return (T?)new XmlSerializer(typeof(T)).Deserialize(stream);
    }

    /// <summary>Reads the file as an XML document model.</summary>
    public XDocument ReadXml()
    {
        using var stream = File.OpenRead(this);
        return XDocument.Load(stream);
    }

    /// <summary>Serializes the value to this file.</summary>
    /// <param name="value">The value serialized with <see cref="System.Xml.Serialization.XmlSerializer"/>. Missing parent directories are not created.</param>
    public void WriteXml<T>(T value)
    {
        using var stream = File.Create(this);
        new XmlSerializer(typeof(T)).Serialize(stream, value);
    }

    /// <summary>Writes an XML document model to this file.</summary>
    public void WriteXml(XDocument value)
    {
        using var stream = File.Create(this);
        value.Save(stream);
    }
}

static class YamlSerialization
{
    public static IDeserializer Deserializer { get; } = new DeserializerBuilder().Build();
    public static ISerializer Serializer { get; } = new SerializerBuilder().Build();

    public static YamlNode? ReadNode(TextReader reader)
    {
        var stream = new YamlStream();
        stream.Load(reader);
        return stream.Documents.Count switch
        {
            0 => null,
            1 => stream.Documents[0].RootNode,
            _ => throw new YamlException("Expected one YAML document."),
        };
    }
}
