using System.Net;
using Xunit;

namespace DotNetDo.Tests;

public sealed class HttpToolCommandTests
{
    [Fact]
    public async Task Awaiting_command_sends_its_request_and_returns_the_semantic_result()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text("""{"versions":["1.0.0","1.1.0"]}""", "application/json"));

        var versions = await new PackageVersions { BaseUrl = server.Url, PackageId = "DotNetDo.Core" };

        Assert.Equal(["1.0.0", "1.1.0"], versions);
        Assert.Equal("/dotnetdo.core/index.json", server.Request.PathAndQuery);
        Assert.Equal("application/json", server.Request.Headers["Accept"]);
    }

    [Fact]
    public async Task Fetching_command_returns_the_raw_response_without_reading_it()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text("not json", status: HttpStatusCode.NotFound));

        var result = await Do.Fetch(new PackageVersions { BaseUrl = server.Url, PackageId = "Missing" }).Completed;

        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("not json", result.ReadText());
    }

    [Fact]
    public async Task Unreadable_response_throws_with_the_raw_response()
    {
        using var server = TestHttpServer.Respond(TestResponse.Text("not json"));

        var exception = await Assert.ThrowsAsync<FetchOutputException>(async () =>
            await new PackageVersions { BaseUrl = server.Url, PackageId = "DotNetDo.Core" });

        Assert.Equal("not json", exception.Result.ReadText());
        Assert.Equal(typeof(string[]), exception.ExpectedType);
    }

    [Fact]
    public void Renders_method_and_url()
    {
        var command = new PackageVersions { BaseUrl = "https://api.nuget.org/v3-flatcontainer/", PackageId = "DotNetDo.Core" };

        Assert.Equal("GET https://api.nuget.org/v3-flatcontainer/dotnetdo.core/index.json", command.ToString());
    }

    sealed record PackageVersions : HttpToolCommand<string[]>
    {
        public PackageVersions() => Headers = [new("Accept", "application/json")];

        public required string BaseUrl { get; init; }
        public required string PackageId { get; init; }

        protected override string Url => $"{BaseUrl}{PackageId.ToLowerInvariant()}/index.json";

        protected override string[] ReadResult(FetchResult result) =>
            result.ReadJson()!["versions"]!.AsArray().Select(version => version!.GetValue<string>()).ToArray();
    }
}
