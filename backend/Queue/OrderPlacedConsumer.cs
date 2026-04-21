namespace backend.Queue;

public class OrderPlacedConsumer : BackgroundService
{
    private readonly InMemoryOrderPlacedQueue _queue;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(InMemoryOrderPlacedQueue queue, ILogger<OrderPlacedConsumer> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var msg in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            _logger.LogInformation(
                "Order {OrderId} placed by buyer {BuyerId} for total {Total} (in-memory queue demo)",
                msg.OrderId,
                msg.BuyerId,
                msg.TotalAmount);
            await Task.Delay(50, stoppingToken);
        }
    }
}
