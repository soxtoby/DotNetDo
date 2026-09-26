# HTTP requests

Build automation often needs to talk to a web service directly: checking which package versions are published, posting a deployment notification, or downloading a tool. DotNetDo's fetch helpers make these requests one-liners and fail the task with a readable error when the server rejects them.

## Read a response

Use `Do.FetchJson()` to request JSON and deserialize the response into your own type:

```csharp
var index = await Do.FetchJson<PackageIndex>(
    "https://api.nuget.org/v3-flatcontainer/dotnetdo.core/index.json");

Log.Information("Latest version is {Version}", index!.Versions[^1]);

record PackageIndex(string[] Versions);
```

Leave out the type argument to get a `JsonNode` when you only need a value or two. `Do.FetchXml()` works the same way for XML, and `Do.FetchText()` returns the body as a string.

These helpers ask the server for their format with an `Accept` header, unless you supply one yourself. If the response status isn't successful, they throw an exception whose message includes the method, URL, status, and the start of the response body.

## Send data

Pass `FetchOptions` to choose the method, add headers, and attach a body. Bodies are `BinaryData` values, so JSON, text, bytes, and files all use the same property:

```csharp
var apiKey = Do.Secret("seq-api-key").Required();

await Do.Fetch("https://seq.example.com/api/events/raw", new()
    {
        Method = HttpMethod.Post,
        Headers = [new("X-Seq-ApiKey", apiKey.Unwrap())],
        Body = BinaryData.FromObjectAsJson(new { Events = events }),
    });
```

`BinaryData.FromObjectAsJson()` sets the JSON content type. For other bodies, give the media type to the `BinaryData` factory or add a `Content-Type` header.

Secrets used in headers or URLs stay masked in DotNetDo's logs. See [Parameters and secrets](parameters-and-secrets.md).

## Check the status yourself

Awaiting `Do.Fetch()` throws for an unsuccessful status, just like awaiting `Do.Exec()` throws for a non-zero exit code. Await `Completed` instead when a failure status is an expected answer:

```csharp
using System.Net;

var release = await Do.Fetch($"https://api.github.com/repos/{repo}/releases/tags/{tag}").Completed;

if (release.StatusCode == HttpStatusCode.NotFound)
    Log.Information("{Tag} has not been released yet", tag);
else
    Log.Information("{Tag} was published at {Url}", tag, release.ReadJson()!["html_url"]);
```

The result has the status code, headers, and body, and it reads JSON, XML, YAML, TOML, or text.

## Download a file

`Do.FetchFile()` writes the response to disk as it arrives, so large downloads don't have to fit in memory. It returns the file path, which you can pass straight to the archive helpers:

```csharp
var downloads = Do.CreateTempDirectory();
var archive = await Do.FetchFile(
    "https://github.com/dotnet/docfx/releases/download/v2.78.3/docfx-win-x64-v2.78.3.zip",
    downloads / "docfx.zip");

archive.UnzipTo(Do.RootDirectory / ".tools" / "docfx");
```

If the request fails, no file is written.

See the [HTTP requests API reference](../reference/core/http.yml) for every option.
