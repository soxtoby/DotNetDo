using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using Tomlyn;
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

    /// <summary>Serializes the value as one YAML document to this file.</summary>
    /// <param name="value">The value serialized as YAML. Missing parent directories are not created.</param>
    /// <param name="serializer">The YAML serializer; <see langword="null"/> uses DotNetDo's default instance.</param>
    public void WriteYaml<T>(T value, ISerializer? serializer = null)
    {
        using var writer = File.CreateText(this);
        (serializer ?? YamlSerialization.Serializer).Serialize(writer, value);
    }

    /// <summary>Deserializes the file into the requested value type.</summary>
    public T? ReadXml<T>()
    {
        using var stream = File.OpenRead(this);
        return (T?)new XmlSerializer(typeof(T)).Deserialize(stream);
    }

    /// <summary>Serializes the value to this file.</summary>
    /// <param name="value">The value serialized with <see cref="System.Xml.Serialization.XmlSerializer"/>. Missing parent directories are not created.</param>
    public void WriteXml<T>(T value)
    {
        using var stream = File.Create(this);
        new XmlSerializer(typeof(T)).Serialize(stream, value);
    }
}

static class YamlSerialization
{
    public static IDeserializer Deserializer { get; } = new DeserializerBuilder().Build();
    public static ISerializer Serializer { get; } = new SerializerBuilder().Build();
}
