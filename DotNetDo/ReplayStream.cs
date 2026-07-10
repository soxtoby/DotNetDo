using System.Threading.Channels;

namespace DotNetDo;

sealed class ReplayStream<T> : IAsyncEnumerable<T>
{
    readonly Lock _gate = new();
    readonly List<T> _items = [];
    readonly List<Channel<T>> _subscribers = [];
    Exception? _exception;
    bool _completed;

    public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<T>();

        lock (_gate)
        {
            foreach (var item in _items)
                channel.Writer.TryWrite(item);

            if (_completed)
                channel.Writer.TryComplete(_exception);
            else
                _subscribers.Add(channel);
        }

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync().WithCancellation(cancellationToken))
                yield return item;
        }
        finally
        {
            lock (_gate)
                _subscribers.Remove(channel);
        }
    }

    public void Append(T item)
    {
        Channel<T>[] subscribers;
        lock (_gate)
        {
            if (!_completed)
            {
                _items.Add(item);
                subscribers = [.._subscribers];
            }
            else
            {
                subscribers = [];
            }
        }

        foreach (var subscriber in subscribers)
            subscriber.Writer.TryWrite(item);
    }

    public void Complete(Exception? exception = null)
    {
        Channel<T>[] subscribers;
        lock (_gate)
        {
            if (_completed)
            {
                subscribers = [];
            }
            else
            {
                _completed = true;
                _exception = exception;
                subscribers = [.._subscribers];
            }
        }

        foreach (var subscriber in subscribers)
            subscriber.Writer.TryComplete(exception);
    }
}