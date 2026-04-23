namespace backend.Models.Enums;

/// <summary>สถานะบรรทัดเมื่อลูกค้าขอยกเลิกแบบแยกร้าน</summary>
public enum OrderLineCancellationState
{
    None = 0,

    /// <summary>รอร้านนี้ตัดสิน (อนุมัติยกเลิกเฉพาะร้าน / ไม่อนุมัติแล้วคงคำสั่งซื้อเดิม)</summary>
    PendingSellerDecision = 1,

    /// <summary>ร้านไม่อนุมัติคำขอยกเลิก — บรรทัดนี้ยังอยู่ในออเดอร์เดิม รอร้านอื่นตัดสินถ้ามี</summary>
    SellerDeclinedCancellation = 2,
}
