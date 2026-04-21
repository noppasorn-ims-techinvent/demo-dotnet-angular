using System.ComponentModel.DataAnnotations;
using backend.Models.Enums;

namespace backend.Models.Dtos;

public class OrderLineDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }

    public int BuyerId { get; set; }

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public IReadOnlyList<OrderLineDto> Lines { get; set; } = Array.Empty<OrderLineDto>();
}

public class PlaceOrderLineRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

public class PlaceOrderRequest
{
    [Required]
    [MinLength(1)]
    public List<PlaceOrderLineRequest> Lines { get; set; } = new();
}

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}
