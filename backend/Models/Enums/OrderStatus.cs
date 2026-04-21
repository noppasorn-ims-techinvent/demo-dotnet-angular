namespace backend.Models.Enums;

public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
    Shipped = 2,
    Cancelled = 3,

    /// <summary>ลูกค้าขอยกเลิก — รอแอดมินหรือเจ้าของสินค้าในออเดอร์พิจารณา</summary>
    CancellationPending = 4,
}
