using backend.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

/// <summary>Endpoint สำหรับดู trace / demo ITrace — ไม่ใช่ re-execute path ของ UseExceptionHandler (ใช้ <see cref="Middleware.ApiExceptionHandler"/> แทน)</summary>
[AllowAnonymous]
public class ErrorController : BaseApiController
{
    private readonly ITraceContext _trace;

    public ErrorController(ITraceContext trace)
    {
        _trace = trace;
    }

    /// <summary>คืน traceId ปัจจุบัน (ใช้เทียบกับ body ของ ApiResponse.traceId หลังเกิด error)</summary>
    [HttpGet("trace")]
    public ActionResult<object> GetTrace()
    {
        return Ok(
            new
            {
                traceId = _trace.TraceId,
                correlationIdHeader = _trace.CorrelationIdHeader,
            });
    }
}
