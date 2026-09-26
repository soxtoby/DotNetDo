using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Xunit;

namespace DotNetDo.Tests;

/// <summary>A loopback HTTP server that records requests and answers with a caller-supplied response.</summary>
sealed class TestHttpServer : IDisposable
{
    readonly HttpListener _listener = new();
    readonly Func<RecordedRequest, TestResponse> _respond;
    readonly Task _serving;
    volatile bool _stopping;

    public TestHttpServer(Func<RecordedRequest, TestResponse> respond)
    {
        _respond = respond;
        Url = $"http://localhost:{FreePort()}/";
        _listener.Prefixes.Add(Url);
        _listener.Start();
        _serving = Task.Run(ServeAsync);
    }

    public string Url { get; }
    public ConcurrentQueue<RecordedRequest> Requests { get; } = new();
    public RecordedRequest Request => Assert.Single(Requests);

    public static TestHttpServer Respond(TestResponse response) => new(_ => response);

    async Task ServeAsync()
    {
        while (!_stopping)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception) when (_stopping)
            {
                return;
            }

            using var body = new MemoryStream();
            await context.Request.InputStream.CopyToAsync(body);
            var request = new RecordedRequest(
                context.Request.HttpMethod,
                context.Request.Url!.PathAndQuery,
                context.Request.Headers.AllKeys.ToDictionary(key => key!, key => context.Request.Headers[key]!, StringComparer.OrdinalIgnoreCase),
                body.ToArray());
            Requests.Enqueue(request);

            var response = _respond(request);
            context.Response.StatusCode = (int)response.Status;
            if (response.ContentType is not null)
                context.Response.ContentType = response.ContentType;
            foreach (var (name, value) in response.Headers ?? new Dictionary<string, string>())
                context.Response.Headers[name] = value;
            await context.Response.OutputStream.WriteAsync(response.Body);
            context.Response.Close();
        }
    }

    static int FreePort()
    {
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        return ((IPEndPoint)socket.LocalEndpoint).Port;
    }

    public void Dispose()
    {
        _stopping = true;
        _listener.Stop();
        _listener.Close();
        _serving.Wait();
    }
}

sealed record RecordedRequest(string Method, string PathAndQuery, IReadOnlyDictionary<string, string> Headers, byte[] Body)
{
    public string BodyText => Encoding.UTF8.GetString(Body);
}

sealed record TestResponse(HttpStatusCode Status, byte[] Body, string? ContentType = null, IReadOnlyDictionary<string, string>? Headers = null)
{
    public static TestResponse Text(string body, string contentType = "text/plain", HttpStatusCode status = HttpStatusCode.OK) =>
        new(status, Encoding.UTF8.GetBytes(body), contentType);
}
