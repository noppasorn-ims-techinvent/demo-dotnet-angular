namespace backend.Infrastructure;

/// <summary>ข้อมูล trace ของ request ปัจจุบัน (สำหรับ log / support / ส่งคืน client)</summary>
public interface ITraceContext
{
    /// <summary>ตรงกับ <see cref="Microsoft.AspNetCore.Http.HttpContext.TraceIdentifier"/> หลัง CorrelationId middleware</summary>
    string TraceId { get; }

    /// <summary>ค่า header X-Correlation-ID ถ้ามี</summary>
    string? CorrelationIdHeader { get; }
}
