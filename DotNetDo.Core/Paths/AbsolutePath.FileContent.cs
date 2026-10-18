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
    /// <summary>Temporarily replaces this file's contents and restores its original bytes when disposed.</summary>
    /// <param name="replace">The operation that replaces this file's contents.</param>
    public IDisposable TemporaryFileContents(Action<AbsolutePath> replace)
    {
        ArgumentNullException.ThrowIfNull(replace);

        var originalContents = File.ReadAllBytes(this);
        try
        {
            replace(this);
        }
        catch (Exception replacementError)
        {
            try
            {
                File.WriteAllBytes(this, originalContents);
            }
            catch (Exception restorationError)
            {
                throw new AggregateException(replacementError, restorationError);
            }

            throw;
        }

        return new FileContentsRestoration(this, originalContents);
    }

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

    /// <summary>Reads the entire file as binary data.</summary>
    /// <param name="mediaType">The media type of the file's contents, such as <c>application/zip</c>; <see langword="null"/> leaves it unset.</param>
    public BinaryData ReadBinary(string? mediaType = null) => BinaryData.FromBytes(File.ReadAllBytes(this), mediaType);

    /// <summary>Writes binary data to this existing file location, replacing its contents.</summary>
    /// <param name="data">The complete file content. Its media type isn't stored. Missing parent directories are not created.</param>
    public void WriteBinary(BinaryData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        File.WriteAllBytes(this, data.ToMemory().Span);
    }

    /// <summary>Copies a stream to this existing file location, replacing its contents, without buffering the stream in memory.</summary>
    /// <remarks>If the copy fails or is canceled, the file is deleted rather than left partly written.</remarks>
    /// <param name="source">The stream to copy from its current position to its end. It is left open. Missing parent directories are not created.</param>
    /// <param name="cancellationToken">Cancels the copy.</param>
    public async Task WriteStream(Stream source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var file = File.Create(this);
        try
        {
            await using (file)
                await source.CopyToAsync(file, cancellationToken);
        }
        catch
        {
            File.Delete(this);
            throw;
        }
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

    /// <summary>Reads the variables assigned in a dotenv (<c>.env</c>) file, in file order.</summary>
    /// <remarks>
    /// Each line is a <c>NAME=value</c> assignment, optionally prefixed with <c>export</c>, a <c>#</c> comment, or blank.
    /// Unquoted values are trimmed and end at a <c>#</c> preceded by whitespace. Single-quoted values are literal.
    /// Double-quoted values support backslash escapes for newline, carriage return, tab, double quote, and backslash. Quoted values may span lines.
    /// Variable references such as <c>$HOME</c> are read as written, never expanded.
    /// A later assignment replaces the value of an earlier one with the same name.
    /// </remarks>
    /// <exception cref="FormatException">A line is not an assignment, comment, or blank line, or a quoted value is not closed.</exception>
    public OrderedDictionary<string, string> ReadDotEnv() => DotEnv.Parse(ReadText());

    /// <summary>Writes variables to this file in dotenv (<c>.env</c>) format, replacing its contents.</summary>
    /// <remarks>
    /// Values are written unquoted when they contain only letters, numbers, and common URL punctuation,
    /// otherwise single-quoted when possible, and double-quoted with escapes as a last resort.
    /// <see cref="ReadDotEnv"/> reads every written value back unchanged.
    /// Use <see cref="UpdateDotEnv"/> to change an existing file while keeping its comments and layout.
    /// </remarks>
    /// <param name="variables">The variable names and values, in file order. Missing parent directories are not created.</param>
    /// <exception cref="ArgumentException">A variable name contains characters other than letters, numbers, <c>_</c>, <c>-</c>, or <c>.</c>. The file is left unchanged.</exception>
    public void WriteDotEnv(IEnumerable<KeyValuePair<string, string>> variables) => WriteText(DotEnv.Format(variables));

    /// <summary>Sets variables in this existing dotenv (<c>.env</c>) file, keeping the rest of the file as it is.</summary>
    /// <remarks>
    /// Variables that aren't supplied keep their values, so <paramref name="variables"/> can hold just the variables to change,
    /// or the result of <see cref="ReadDotEnv"/> after changing it. Updating never removes a variable.
    /// Only the values of changed variables are rewritten in their existing quote style when they can represent the new value.
    /// Comments, blank lines, line order, <c>export</c> prefixes, inline comments, line endings, and encoding are kept.
    /// New variables are appended to the end of the file. Existing lines aren't moved, whatever order the variables are supplied in.
    /// When a name is assigned more than once, changing it rewrites only the last assignment, which is the one <see cref="ReadDotEnv"/> uses.
    /// </remarks>
    /// <param name="variables">The variables to set. Variables that aren't already in the file are added.</param>
    /// <exception cref="FileNotFoundException">The file doesn't exist. Use <see cref="WriteDotEnv"/> to create one.</exception>
    /// <exception cref="FormatException">The existing file isn't valid dotenv content.</exception>
    /// <exception cref="ArgumentException">A new variable name contains characters other than letters, numbers, <c>_</c>, <c>-</c>, or <c>.</c>. The file is left unchanged.</exception>
    public void UpdateDotEnv(IEnumerable<KeyValuePair<string, string>> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);

        var (text, encoding) = ReadTextAndEncoding();
        WriteText(DotEnv.Update(text, variables), encoding);
    }

    (string Text, Encoding Encoding) ReadTextAndEncoding()
    {
        using var reader = new StreamReader(this, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), detectEncodingFromByteOrderMarks: true);
        return (reader.ReadToEnd(), reader.CurrentEncoding);
    }

    sealed class FileContentsRestoration(AbsolutePath path, byte[] contents) : IDisposable
    {
        readonly Lock _sync = new();
        bool _restored;

        public void Dispose()
        {
            lock (_sync)
            {
                if (_restored)
                    return;

                File.WriteAllBytes(path, contents);
                _restored = true;
            }
        }
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
