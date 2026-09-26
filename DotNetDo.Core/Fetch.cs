using System.Collections.Immutable;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Serilog;
using Tomlyn;
using Tomlyn.Model;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace DotNetDo;

public static partial class Do
{
    static readonly HttpClient HttpClient = new(new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(5) });
    static readonly Lazy<string> DefaultUserAgent = new(() =>
        {
            var version = typeof(Do).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? typeof(Do).Assembly.GetName().Version!.ToString(3);
            return $"DotNetDo/{version.Split('+')[0]}";
        });

    /// <summary>Sends an HTTP request immediately and buffers its response.</summary>
    /// <param name="url">The absolute request URL.</param>
    /// <param name="options">Optional method, headers, and body.</param>
    public static FetchRequest Fetch(string url, FetchOptions? options = null) => 
        new(SendAsync(url, options ?? new FetchOptions()));

    /// <summary>Requests JSON and deserializes the successful response into the requested value type.</summary>
    /// <param name="url">The absolute request URL.</param>
    /// <param name="options">Optional method, headers, and body. A caller-supplied <c>Accept</c> header replaces <c>application/json</c>.</param>
    /// <param name="jsonOptions">Options for deserializing the response body.</param>
    public static async Task<T?> FetchJson<T>(string url, FetchOptions? options = null, JsonSerializerOptions? jsonOptions = null) =>
        (await Fetch(url, Accepting(options, "application/json"))).ReadJson<T>(jsonOptions);

    /// <summary>Requests JSON and reads the successful response as a JSON document model.</summary>
    /// <param name="url">The absolute request URL.</param>
    /// <param name="options">Optional method, headers, and body. A caller-supplied <c>Accept</c> header replaces <c>application/json</c>.</param>
    /// <param name="jsonOptions">Options for parsing the response body.</param>
    public static async Task<JsonNode?> FetchJson(string url, FetchOptions? options = null, JsonSerializerOptions? jsonOptions = null) =>
        (await Fetch(url, Accepting(options, "application/json"))).ReadJson(jsonOptions);

    /// <summary>Requests XML and deserializes the successful response into the requested value type.</summary>
    /// <param name="url">The absolute request URL.</param>
    /// <param name="options">Optional method, headers, and body. A caller-supplied <c>Accept</c> header replaces <c>application/xml</c>.</param>
    public static async Task<T?> FetchXml<T>(string url, FetchOptions? options = null) =>
        (await Fetch(url, Accepting(options, "application/xml"))).ReadXml<T>();

    /// <summary>Requests XML and reads the successful response as an XML document model.</summary>
    /// <param name="url">The absolute request URL.</param>
    /// <param name="options">Optional method, headers, and body. A caller-supplied <c>Accept</c> header replaces <c>application/xml</c>.</param>
    public static async Task<XDocument> FetchXml(string url, FetchOptions? options = null) =>
        (await Fetch(url, Accepting(options, "application/xml"))).ReadXml();

    /// <summary>Decodes the successful response as text.</summary>
    /// <param name="url">The absolute request URL.</param>
    /// <param name="options">Optional method, headers, and body.</param>
    public static async Task<string> FetchText(string url, FetchOptions? options = null) =>
        (await Fetch(url, options)).ReadText();

    /// <summary>Streams the successful response body to a file, replacing any existing file.</summary>
    /// <param name="url">The absolute request URL.</param>
    /// <param name="path">The destination file. Missing parent directories are not created.</param>
    /// <param name="options">Optional method, headers, and body.</param>
    /// <returns>The destination file.</returns>
    public static async Task<AbsolutePath> FetchFile(string url, AbsolutePath path, FetchOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        options ??= new FetchOptions();
        using var response = await SendAsync(url, options, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
            throw new FetchFailedException(await ReadResultAsync(url, options, response));

        var file = File.Create(path);
        try
        {
            await using (file)
                await response.Content.CopyToAsync(file);
        }
        catch
        {
            File.Delete(path);
            throw;
        }

        return path;
    }

    static FetchOptions Accepting(FetchOptions? options, string mediaType)
    {
        options ??= new FetchOptions();
        return options.Headers.Any(header => header.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase))
            ? options
            : options with { Headers = [..options.Headers, new("Accept", mediaType)] };
    }

    static async Task<FetchResult> SendAsync(string url, FetchOptions options)
    {
        using var response = await SendAsync(url, options, HttpCompletionOption.ResponseContentRead);
        return await ReadResultAsync(url, options, response);
    }

    static async Task<HttpResponseMessage> SendAsync(string url, FetchOptions options, HttpCompletionOption completion)
    {
        using var request = CreateRequest(url, options);
        Log.Debug("Fetching {Method} {Url}", options.Method, url);

        try
        {
            return await HttpClient.SendAsync(request, completion);
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Failed to fetch {Method} {Url}", options.Method, url);
            throw;
        }
    }

    static async Task<FetchResult> ReadResultAsync(string url, FetchOptions options, HttpResponseMessage response) =>
        new()
            {
                Method = options.Method,
                Url = url,
                StatusCode = response.StatusCode,
                Headers = response.Headers
                    .Concat(response.Content.Headers)
                    .ToImmutableDictionary(
                        header => header.Key,
                        IReadOnlyList<string> (header) => [..header.Value],
                        StringComparer.OrdinalIgnoreCase),
                Body = BinaryData.FromBytes(
                    await response.Content.ReadAsByteArrayAsync(),
                    response.Content.Headers.ContentType?.MediaType),
            };

    static HttpRequestMessage CreateRequest(string url, FetchOptions options)
    {
        var request = new HttpRequestMessage(options.Method, url);

        if (options.Body is { } body)
        {
            request.Content = new ReadOnlyMemoryContent(body.ToMemory());
            if (body.MediaType is { } mediaType)
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(mediaType);
        }

        var headerContentType = false;
        foreach (var (name, value) in options.Headers)
        {
            if (request.Headers.TryAddWithoutValidation(name, value))
                continue;

            var content = request.Content
                ?? throw new ArgumentException($"The '{name}' header describes a request body, but no body was supplied.", nameof(options));

            if (name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) && !headerContentType)
            {
                content.Headers.ContentType = null;
                headerContentType = true;
            }

            content.Headers.TryAddWithoutValidation(name, value);
        }

        if (!request.Headers.Contains("User-Agent"))
            request.Headers.TryAddWithoutValidation("User-Agent", DefaultUserAgent.Value);

        return request;
    }
}

/// <summary>Controls an HTTP request's method, headers, and body.</summary>
public record FetchOptions
{
    /// <summary>The request method; defaults to GET.</summary>
    public HttpMethod Method { get; init; } = HttpMethod.Get;
    /// <summary>Request headers in sending order. Content headers such as <c>Content-Type</c> apply to the body.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Headers { get; init => field = [.. value]; } = [];
    /// <summary>The request body; its media type becomes the <c>Content-Type</c> unless a header supplies one.</summary>
    public BinaryData? Body { get; init; }
}

/// <summary>A sent HTTP request with separate completion and success tasks.</summary>
public sealed class FetchRequest
{
    internal FetchRequest(Task<FetchResult> completed)
    {
        Completed = completed;
        Succeeded = EnsureSuccessAsync(completed);
    }

    /// <summary>Completes with the response for every status code.</summary>
    public Task<FetchResult> Completed { get; }
    /// <summary>Completes with the response only for a success status code; otherwise throws <see cref="FetchFailedException"/>.</summary>
    public Task<FetchResult> Succeeded { get; }

    /// <summary>Allows awaiting the request and throws when the response status is unsuccessful.</summary>
    public TaskAwaiter<FetchResult> GetAwaiter() => Succeeded.GetAwaiter();

    static async Task<FetchResult> EnsureSuccessAsync(Task<FetchResult> completed)
    {
        var result = await completed;
        return result.IsSuccessStatusCode
            ? result
            : throw new FetchFailedException(result);
    }
}

/// <summary>The buffered response to an HTTP request.</summary>
public sealed record FetchResult
{
    /// <summary>The request method.</summary>
    public required HttpMethod Method { get; init; }
    /// <summary>The absolute request URL.</summary>
    public required string Url { get; init; }
    /// <summary>The response status code.</summary>
    public required HttpStatusCode StatusCode { get; init; }
    /// <summary>Response and content headers, with case-insensitive names.</summary>
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> Headers { get; init; }
    /// <summary>The buffered response body, whose media type comes from the <c>Content-Type</c> header.</summary>
    public required BinaryData Body { get; init; }

    /// <summary>Whether the status code is in the 2xx range.</summary>
    public bool IsSuccessStatusCode => (int)StatusCode is >= 200 and <= 299;

    /// <summary>Decodes the response body using its <c>Content-Type</c> charset or byte-order mark, falling back to UTF-8.</summary>
    public string ReadText()
    {
        using var reader = new StreamReader(Body.ToStream(), CharsetEncoding() ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    /// <summary>Deserializes the response body into the requested value type.</summary>
    public T? ReadJson<T>(JsonSerializerOptions? options = null) => TextContent.ReadJson<T>(ReadText(), options);

    /// <summary>Reads the response body as a JSON document model.</summary>
    public JsonNode? ReadJson(JsonSerializerOptions? options = null) => TextContent.ReadJson(ReadText(), options);

    /// <summary>Deserializes the response body into the requested value type.</summary>
    public T? ReadToml<T>(TomlSerializerOptions? options = null) => TextContent.ReadToml<T>(ReadText(), options);

    /// <summary>Reads the response body as a TOML document model.</summary>
    public TomlTable ReadToml(TomlSerializerOptions? options = null) => TextContent.ReadToml(ReadText(), options);

    /// <summary>Deserializes one YAML document from the response body.</summary>
    public T? ReadYaml<T>(IDeserializer? deserializer = null) => TextContent.ReadYaml<T>(ReadText(), deserializer);

    /// <summary>Reads the root node of one YAML document from the response body.</summary>
    public YamlNode? ReadYaml() => TextContent.ReadYaml(ReadText());

    /// <summary>Deserializes the response body into the requested value type.</summary>
    public T? ReadXml<T>() => TextContent.ReadXml<T>(ReadText());

    /// <summary>Reads the response body as an XML document model.</summary>
    public XDocument ReadXml() => TextContent.ReadXml(ReadText());

    Encoding? CharsetEncoding()
    {
        if (!Headers.TryGetValue("Content-Type", out var values)
            || !MediaTypeHeaderValue.TryParse(values.FirstOrDefault(), out var contentType)
            || contentType.CharSet is not { } charset)
            return null;

        try
        {
            return Encoding.GetEncoding(charset.Trim('"'));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}

/// <summary>Thrown when an awaited HTTP request receives an unsuccessful status code.</summary>
public sealed class FetchFailedException(FetchResult result) : Exception(FailureMessage(result))
{
    const int MaxBodyLength = 1000;

    /// <summary>The unsuccessful response.</summary>
    public FetchResult Result { get; } = result;

    static string FailureMessage(FetchResult result)
    {
        var message = $"{result.Method} {result.Url} failed with status {(int)result.StatusCode} ({result.StatusCode})";
        var body = result.ReadText();
        return body.Length switch
            {
                0 => message + ".",
                > MaxBodyLength => $"{message}: {body[..MaxBodyLength]}...",
                _ => $"{message}: {body}",
            };
    }
}
