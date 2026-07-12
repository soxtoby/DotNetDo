using System.Text;
using System.Text.Json;
using System.Xml.Serialization;
using Tomlyn;

namespace DotNetDo;

public sealed partial record AbsolutePath
{
    public string ReadText(Encoding? encoding = null) =>
        encoding is null ? File.ReadAllText(this) : File.ReadAllText(this, encoding);

    public string[] ReadLines(Encoding? encoding = null) =>
        encoding is null ? File.ReadAllLines(this) : File.ReadAllLines(this, encoding);

    public void WriteText(string text, Encoding? encoding = null)
    {
        if (encoding is null)
            File.WriteAllText(this, text);
        else
            File.WriteAllText(this, text, encoding);
    }

    public void WriteLines(IEnumerable<string> lines, Encoding? encoding = null)
    {
        if (encoding is null)
            File.WriteAllLines(this, lines);
        else
            File.WriteAllLines(this, lines, encoding);
    }

    public T? ReadJson<T>(JsonSerializerOptions? options = null)
    {
        using var stream = File.OpenRead(this);
        return JsonSerializer.Deserialize<T>(stream, options);
    }

    public void WriteJson<T>(T value, JsonSerializerOptions? options = null)
    {
        using var stream = File.Create(this);
        JsonSerializer.Serialize(stream, value, options);
    }

    public T? ReadToml<T>(TomlSerializerOptions? options = null)
    {
        using var stream = File.OpenRead(this);
        return TomlSerializer.Deserialize<T>(stream, options);
    }

    public void WriteToml<T>(T value, TomlSerializerOptions? options = null)
    {
        using var stream = File.Create(this);
        TomlSerializer.Serialize(stream, value, options);
    }

    public T? ReadXml<T>()
    {
        using var stream = File.OpenRead(this);
        return (T?)new XmlSerializer(typeof(T)).Deserialize(stream);
    }

    public void WriteXml<T>(T value)
    {
        using var stream = File.Create(this);
        new XmlSerializer(typeof(T)).Serialize(stream, value);
    }
}
