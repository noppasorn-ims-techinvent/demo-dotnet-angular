using backend.Models.Enums;

namespace backend.Models.Entities;

public class Order : BaseEntity
{
    public int BuyerId { get; set; }

    public User Buyer { get; set; } = null!;

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>สถานะก่อนขอยกเลิก (Pending หรือ Paid) — ใช้คืนสถานะเมื่อไม่อนุมัติ >ยกเลิก</summary>
    public OrderStatus? PreCancellationStatus { get; set; }

    public string? BuyerCancellationReason { get; set; }

    public string? CancellationReviewerNote { get; set; }

    public int? CancellationReviewedByUserId { get; set; }

    /// <summary>จำลองการชำระ: card / bank</summary>
    public string? SimulatedPaymentMethod { get; set; }

    /// <summary>ออเดอร์นี้แยกมาจากคำสั่งซื้อ # หลังร้านไม่อนุมัติยกเลิก (ไม่ใช้ FK ถ้าออเดอร์ต้นทางถูกลบ)</summary>
    public int? SplitSourceOrderId { get; set; }

    public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
}
