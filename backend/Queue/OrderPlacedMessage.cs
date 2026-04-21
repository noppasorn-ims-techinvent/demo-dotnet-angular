namespace backend.Queue;

public record OrderPlacedMessage(int OrderId, int BuyerId, decimal TotalAmount);
