using System.Runtime.CompilerServices;

namespace DotNetDo;

public static partial class Do
{
    /// <summary>Sends the command's request immediately and buffers its response without reading a semantic result.</summary>
    /// <param name="command">A typed HTTP command whose URL, method, headers, and body are used.</param>
    internal static FetchRequest Fetch(HttpToolCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Fetch(command.RequestUrl, command);
    }
}

/// <summary>Base value object for describing and awaiting a request to an HTTP API.</summary>
abstract record HttpToolCommand : FetchOptions
{
    /// <summary>Gets the absolute request URL derived from the command's properties.</summary>
    protected abstract string Url { get; }

    internal string RequestUrl => Url;

    /// <inheritdoc />
    public sealed override string ToString() => $"{Method} {Url}";
}

/// <summary>An HTTP command whose await produces a semantic result.</summary>
abstract record HttpToolCommand<TResult> : HttpToolCommand
{
    /// <summary>Allows awaiting the command's semantic result.</summary>
    public TaskAwaiter<TResult> GetAwaiter() => ExecuteAsync().GetAwaiter();

    async Task<TResult> ExecuteAsync()
    {
        var result = await ExecuteRequestAsync();

        try
        {
            return ReadResult(result);
        }
        catch (FetchOutputException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new FetchOutputException(result, typeof(TResult), exception);
        }
    }

    /// <summary>Sends the request and returns its successful raw response.</summary>
    protected virtual async Task<FetchResult> ExecuteRequestAsync() => await Do.Fetch(this);

    /// <summary>Converts a successful raw response to the command's semantic result.</summary>
    protected abstract TResult ReadResult(FetchResult result);
}

/// <summary>Indicates that a successful HTTP response could not be converted to its semantic result.</summary>
sealed class FetchOutputException(FetchResult result, Type expectedType, Exception innerException)
    : Exception($"{result.Method} {result.Url} produced a response that could not be read as {expectedType.Name}.", innerException)
{
    /// <summary>The raw successful response, retained for inspection.</summary>
    public FetchResult Result { get; } = result;

    /// <summary>The semantic result type expected by the HTTP command.</summary>
    public Type ExpectedType { get; } = expectedType;
}
