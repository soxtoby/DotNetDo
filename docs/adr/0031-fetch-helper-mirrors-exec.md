# Fetch helper mirrors Exec

`Do.Fetch` is the HTTP counterpart to `Do.Exec`. It sends the request immediately and returns a `FetchRequest` whose `Completed` task yields a buffered `FetchResult` for every status, and whose `Succeeded` task (used when awaiting) throws `FetchFailedException` for a non-2xx status. `FetchResult` offers the same `ReadText`, `ReadJson`, `ReadXml`, `ReadYaml`, and `ReadToml` readers as `ExecResult`, backed by one shared implementation. Like the Exec readers, they don't check the status. The failure message includes the method, URL, status, and up to 1000 characters of the response body, because build logs often show only the exception message.

One-line reads go through format-specific shortcuts: `Do.FetchJson`, `Do.FetchXml`, and `Do.FetchText`. They require a successful status and return the parsed body. The JSON and XML shortcuts add an `Accept` header for their format unless the caller supplies one. `Do.Fetch` itself sends no default `Accept`, so downloads and custom media types aren't affected. `Do.FetchFile` is the only operation that streams the response body. It writes to disk only after a successful status, and deletes a partial file if the transfer fails.

Request bodies are `System.BinaryData` values. `BinaryData` is immutable and can be sent repeatedly, so options records reused through `with` stay valid. Its media type becomes the `Content-Type` unless a header supplies one. `HttpContent` is not accepted because it can only be sent once and must be disposed. Headers are an ordered list of name/value pairs, copied when the options are created. Content headers apply to the body. Credentials go through ordinary headers; secret redaction masks registered values anywhere in a logged string.

Every request uses one shared `HttpClient`. Requests identify themselves with a `DotNetDo/<version>` user agent unless the caller supplies one, since some APIs, including GitHub's, reject requests without one.

`HttpToolCommand : FetchOptions` is the HTTP counterpart to `ToolCommand : ExecOptions`. A concrete command derives its URL from its properties. `HttpToolCommand<TResult>` converts the successful response in `ReadResult`, wrapping conversion failures in `FetchOutputException`, and `Do.Fetch(command)` returns the raw response. These types remain internal and undocumented until DotNetDo ships an HTTP-based tool, so their shape can change freely until then.

## Considered options

- Sending nothing until the request is used, with `Accept`-setting readers on the request (`await Do.Fetch(url).ReadJson<T>()`). Rejected so that `Do.Fetch` behaves like `Do.Exec`.
- An `object` body that serializes non-content values as JSON. Rejected as ambiguous for strings and less discoverable than `BinaryData` factories.
- Dedicated `Authorization` or bearer-token properties. Rejected because headers cover them and redaction already masks the token.
- Retries, timeouts, and cancellation. Deferred; `Do.Exec` has none either.
