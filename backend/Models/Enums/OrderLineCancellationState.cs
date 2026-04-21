namespace backend.Models.Enums;

/// <summary>สถานะบรรทัดเมื่อลูกค้าขอยกเลิกแบบแยกร้าน</summary>
public enum OrderLineCancellationState
{
    None = 0,

    /// <summary>รอร้านนี้ตัดสิน (อนุมัติยกเลิกเฉพาะร้าน / ไม่อนุมัติแล้วแยกออเดอร์)</summary>
    PendingSellerDecision = 1,
}
