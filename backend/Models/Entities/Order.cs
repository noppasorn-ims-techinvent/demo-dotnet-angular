using backend.Models.Enums;

namespace backend.Models.Entities;

public class Order : BaseEntity
{
    public int BuyerId { get; set; }

    public User Buyer { get; set; } = null!;

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
}
