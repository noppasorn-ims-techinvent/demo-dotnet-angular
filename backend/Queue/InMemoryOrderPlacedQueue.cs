using System.Threading.Channels;

namespace backend.Queue;

public class InMemoryOrderPlacedQueue : IOrderPlacedQueue
{
    private readonly Channel<OrderPlacedMessage> _channel = Channel.CreateUnbounded<OrderPlacedMessage>();

    public ChannelReader<OrderPlacedMessage> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(OrderPlacedMessage message, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(message, cancellationToken);
    }
}
