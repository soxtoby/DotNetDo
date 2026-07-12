using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using Tomlyn;

namespace DotNetDo;

public sealed partial record AbsolutePath
{
    /// <summary>Reads the entire file as text using the supplied encoding or UTF-8.</summary>
    public string ReadText(Encoding? encoding = null) =>
        encoding is null ? File.ReadAllText(this) : File.ReadAllText(this, encoding);

    /// <summary>Reads all file lines using the supplied encoding or UTF-8.</summary>
    public string[] ReadLines(Encoding? encoding = null) =>
        encoding is null ? File.ReadAllLines(this) : File.ReadAllLines(this, encoding);

    /// <summary>Writes text to this existing file location using the supplied encoding or UTF-8.</summary>
    public void WriteText(string text, Encoding? encoding = null)
    {
        if (encoding is null)
            File.WriteAllText(this, text);
        else
            File.WriteAllText(this, text, encoding);
    }

    /// <summary>Writes lines to this existing file location using the supplied encoding or UTF-8.</summary>
    public void WriteLines(IEnumerable<string> lines, Encoding? encoding = null)
    {
        if (encoding is null)
            File.WriteAllLines(this, lines);
        else
            File.WriteAllLines(this, lines, encoding);
    }

    /// <summary>Deserializes the file into the requested value type.</summary>
    public T? ReadJson<T>(JsonSerializerOptions? options = null)
    {
        using var stream = File.OpenRead(this);
        return JsonSerializer.Deserialize<T>(stream, options);
    }

    /// <summary>Serializes the value to this file.</summary>
    public void WriteJson<T>(T value, JsonSerializerOptions? options = null)
    {
        using var stream = File.Create(this);
        JsonSerializer.Serialize(stream, value, options);
    }

    /// <summary>Deserializes the file into the requested value type.</summary>
    public T? ReadToml<T>(TomlSerializerOptions? options = null)
    {
        using var stream = File.OpenRead(this);
        return TomlSerializer.Deserialize<T>(stream, options);
    }

    /// <summary>Serializes the value to this file.</summary>
    public void WriteToml<T>(T value, TomlSerializerOptions? options = null)
    {
        using var stream = File.Create(this);
        TomlSerializer.Serialize(stream, value, options);
    }

    /// <summary>Deserializes the file into the requested value type.</summary>
    public T? ReadXml<T>()
    {
        using var stream = File.OpenRead(this);
        return (T?)new XmlSerializer(typeof(T)).Deserialize(stream);
    }

    /// <summary>Serializes the value to this file.</summary>
    public void WriteXml<T>(T value)
    {
        using var stream = File.Create(this);
        new XmlSerializer(typeof(T)).Serialize(stream, value);
    }
}
