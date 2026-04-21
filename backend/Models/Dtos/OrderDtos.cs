using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using backend.Models.Enums;

namespace backend.Models.Dtos;

public class OrderLineDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public int SellerId { get; set; }

    public string SellerDisplayName { get; set; } = string.Empty;

    /// <summary>OrderLineCancellationState — 1 = รอร้านนี้ตัดสิน</summary>
    public int LineCancellationState { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }

    public int BuyerId { get; set; }

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public IReadOnlyList<OrderLineDto> Lines { get; set; } = Array.Empty<OrderLineDto>();

    public OrderStatus? PreCancellationStatus { get; set; }

    public string? BuyerCancellationReason { get; set; }

    public string? CancellationReviewerNote { get; set; }

    public int? CancellationReviewedByUserId { get; set; }

    public string? SimulatedPaymentMethod { get; set; }

    public int? SplitSourceOrderId { get; set; }
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

public class SimulatePaymentRequest
{
    /// <summary>card | bank</summary>
    [Required]
    [MaxLength(16)]
    public string PaymentMethod { get; set; } = string.Empty;

    [MaxLength(32)]
    public string? CardNumber { get; set; }

    [MaxLength(120)]
    public string? CardHolder { get; set; }

    [MaxLength(5)]
    public string? CardExpiry { get; set; }

    [MaxLength(4)]
    public string? CardCvv { get; set; }

    [MaxLength(32)]
    public string? BankCode { get; set; }

    [MaxLength(32)]
    public string? BankAccountNumber { get; set; }
}

public class RequestCancellationRequest
{
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class ReviewCancellationRequest
{
    public bool Approved { get; set; }

    [Required]
    [MaxLength(500)]
    public string Note { get; set; } = string.Empty;

    /// <summary>เมื่อเป็น Admin ต้องระบุว่าพิจารณาคิวของผู้ขาย user id ใด — ผู้ขายไม่ต้องส่ง</summary>
    [JsonPropertyName("targetSellerUserId")]
    public int? TargetSellerUserId { get; set; }
}
