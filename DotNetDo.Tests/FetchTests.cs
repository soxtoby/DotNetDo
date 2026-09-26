using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using YamlDotNet.RepresentationModel;

namespace DotNetDo.Tests;

public sealed class FetchTests
{
    [Fact]
    public async Task Awaiting_fetch_sends_a_get_request_and_returns_the_response()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text("hello"));

        var result = await Do.Fetch(server.Url + "greeting");

        Assert.Equal("GET", server.Request.Method);
        Assert.Equal("/greeting", server.Request.PathAndQuery);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("hello", result.ReadText());
    }

    [Fact]
    public async Task Awaiting_fetch_throws_with_the_response_body_when_the_status_is_unsuccessful()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text("""{"message":"Validation Failed"}""", "application/json", HttpStatusCode.UnprocessableEntity));

        var exception = await Assert.ThrowsAsync<FetchFailedException>(async () => await Do.Fetch(server.Url + "releases"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, exception.Result.StatusCode);
        Assert.Equal($"GET {server.Url}releases failed with status 422 (UnprocessableEntity): {{\"message\":\"Validation Failed\"}}", exception.Message);
    }

    [Fact]
    public async Task Sends_method_headers_and_body_with_its_media_type()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text("", status: HttpStatusCode.Created));

        await Do.Fetch(server.Url + "api/events", new()
            {
                Method = HttpMethod.Post,
                Headers = [new("X-Api-Key", "abc123")],
                Body = BinaryData.FromObjectAsJson(new { Name = "release" }),
            });

        Assert.Equal("POST", server.Request.Method);
        Assert.Equal("abc123", server.Request.Headers["X-Api-Key"]);
        Assert.Equal("application/json", server.Request.Headers["Content-Type"]);
        Assert.Equal("""{"Name":"release"}""", server.Request.BodyText);
    }

    [Fact]
    public async Task Content_headers_apply_to_the_body()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text(""));

        await Do.Fetch(server.Url, new()
            {
                Method = HttpMethod.Put,
                Headers = [new("Content-Type", "text/csv"), new("Content-Language", "en")],
                Body = BinaryData.FromString("a,b"),
            });

        Assert.Equal("text/csv", server.Request.Headers["Content-Type"]);
        Assert.Equal("en", server.Request.Headers["Content-Language"]);
        Assert.Equal("a,b", server.Request.BodyText);
    }

    [Fact]
    public async Task Headers_are_snapshotted_when_options_are_created()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text(""));
        List<KeyValuePair<string, string>> headers = [new("X-Stage", "before")];
        var options = new FetchOptions { Headers = headers };
        headers[0] = new("X-Stage", "after");

        await Do.Fetch(server.Url, options);

        Assert.Equal("before", server.Request.Headers["X-Stage"]);
    }

    [Fact]
    public async Task Result_exposes_response_headers_and_body_media_type()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text("{}", "application/json; charset=utf-8") with
            {
                Headers = new Dictionary<string, string> { ["ETag"] = "\"v1\"" },
            });

        var result = await Do.Fetch(server.Url);

        Assert.Equal(["\"v1\""], result.Headers["etag"]);
        Assert.Equal(["application/json; charset=utf-8"], result.Headers["Content-Type"]);
        Assert.Equal("application/json", result.Body.MediaType);
    }

    [Fact]
    public async Task Reads_text_using_the_response_charset()
    {
        using var server = TestHttpServer.Respond(new TestResponse(HttpStatusCode.OK, Encoding.Unicode.GetBytes("héllo"), "text/plain; charset=utf-16"));

        var result = await Do.Fetch(server.Url);

        Assert.Equal("héllo", result.ReadText());
    }

    [Fact]
    public async Task Reads_structured_response_bodies()
    {
        using var server = new TestHttpServer(request => TestResponse.Text(request.PathAndQuery switch
            {
                "/json" => """{"Value":"json"}""",
                "/xml" => "<Content><Value>xml</Value></Content>",
                "/yaml" => "Value: yaml",
                "/toml" => "Value = \"toml\"",
                _ => throw new ArgumentException(request.PathAndQuery),
            }));

        Assert.Equal("json", (await Do.Fetch(server.Url + "json")).ReadJson<Content>()!.Value);
        Assert.Equal("json", (await Do.Fetch(server.Url + "json")).ReadJson()!["Value"]!.GetValue<string>());
        Assert.Equal("xml", (await Do.Fetch(server.Url + "xml")).ReadXml<Content>()!.Value);
        Assert.Equal("xml", (await Do.Fetch(server.Url + "xml")).ReadXml().Root!.Element("Value")!.Value);
        Assert.Equal("yaml", (await Do.Fetch(server.Url + "yaml")).ReadYaml<Content>()!.Value);
        Assert.Equal("yaml", ((YamlMappingNode)(await Do.Fetch(server.Url + "yaml")).ReadYaml()!).Children[new YamlScalarNode("Value")].ToString());
        Assert.Equal("toml", (await Do.Fetch(server.Url + "toml")).ReadToml<Content>()!.Value);
        Assert.Equal("toml", (await Do.Fetch(server.Url + "toml")).ReadToml()["Value"]);
    }

    [Fact]
    public async Task Fetch_json_accepts_json_and_deserializes_the_response()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text("""{"value":"json"}""", "application/json"));

        var content = await Do.FetchJson<Content>(server.Url, jsonOptions: new(JsonSerializerDefaults.Web));
        var document = await Do.FetchJson(server.Url);

        Assert.Equal("json", content!.Value);
        Assert.Equal("json", document!["value"]!.GetValue<string>());
        Assert.All(server.Requests, request => Assert.Equal("application/json", request.Headers["Accept"]));
    }

    [Fact]
    public async Task Fetch_xml_accepts_xml_and_deserializes_the_response()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text("<Content><Value>xml</Value></Content>", "application/xml"));

        var content = await Do.FetchXml<Content>(server.Url);
        var document = await Do.FetchXml(server.Url);

        Assert.Equal("xml", content!.Value);
        Assert.Equal("xml", document.Root!.Element("Value")!.Value);
        Assert.All(server.Requests, request => Assert.Equal("application/xml", request.Headers["Accept"]));
    }

    [Fact]
    public async Task Fetch_text_returns_the_response_text()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text("v1.2.3"));

        Assert.Equal("v1.2.3", await Do.FetchText(server.Url));
        Assert.False(server.Request.Headers.ContainsKey("Accept"));
    }

    [Fact]
    public async Task Shortcuts_keep_a_caller_supplied_accept_header()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text("{}", "application/json"));

        await Do.FetchJson(server.Url, new() { Headers = [new("accept", "application/vnd.github+json")] });

        Assert.Equal("application/vnd.github+json", server.Request.Headers["Accept"]);
    }

    [Fact]
    public async Task Shortcuts_throw_when_the_status_is_unsuccessful()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text("nope", status: HttpStatusCode.Unauthorized));

        var exception = await Assert.ThrowsAsync<FetchFailedException>(() => Do.FetchJson(server.Url));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.Result.StatusCode);
    }

    [Fact]
    public async Task Fetch_file_writes_the_response_body_to_the_path()
    {
        byte[] bytes = [0x50, 0x4B, 0x03, 0x04, 0xFF, 0x00];
        using var server = TestHttpServer.Respond(new TestResponse(HttpStatusCode.OK, bytes, "application/zip"));
        var path = Do.CreateTempDirectory() / "tool.zip";

        var written = await Do.FetchFile(server.Url + "tool.zip", path);

        Assert.Equal(path, written);
        Assert.Equal(bytes, File.ReadAllBytes(path));
    }

    [Fact]
    public async Task Fetch_file_throws_without_writing_when_the_status_is_unsuccessful()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text("no such asset", status: HttpStatusCode.NotFound));
        var path = Do.CreateTempDirectory() / "tool.zip";

        var exception = await Assert.ThrowsAsync<FetchFailedException>(() => Do.FetchFile(server.Url, path));

        Assert.Equal("no such asset", exception.Result.ReadText());
        Assert.False(path.Exists);
    }

    [Fact]
    public async Task Identifies_dotnetdo_as_the_user_agent_unless_the_caller_supplies_one()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text(""));

        await Do.Fetch(server.Url);
        await Do.Fetch(server.Url, new() { Headers = [new("User-Agent", "release-bot/1.0")] });

        Assert.Collection(server.Requests,
            request => Assert.Matches(@"^DotNetDo/\d+\.\d+\.\d+", request.Headers["User-Agent"]),
            request => Assert.Equal("release-bot/1.0", request.Headers["User-Agent"]));
    }

    [Fact]
    public async Task Completed_returns_unsuccessful_responses_without_throwing()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text("missing", status: HttpStatusCode.NotFound));

        var result = await Do.Fetch(server.Url).Completed;

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.False(result.IsSuccessStatusCode);
        Assert.Equal("missing", result.ReadText());
    }

    public sealed class Content
    {
        public string? Value { get; set; }
    }
}
