using Microsoft.AspNetCore.Http;

namespace backend.Infrastructure;

public sealed class HttpTraceContext : ITraceContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTraceContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string TraceId => _httpContextAccessor.HttpContext?.TraceIdentifier ?? string.Empty;

    public string? CorrelationIdHeader =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault();
}
