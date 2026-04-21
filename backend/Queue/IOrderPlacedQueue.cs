namespace backend.Queue;

public interface IOrderPlacedQueue
{
    ValueTask EnqueueAsync(OrderPlacedMessage message, CancellationToken cancellationToken = default);
}
