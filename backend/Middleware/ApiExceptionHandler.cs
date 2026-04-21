using System.Text.Json;
using backend.Models.Api;
using Microsoft.AspNetCore.Diagnostics;

namespace backend.Middleware;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        httpContext.Response.ContentType = "application/json";
        var traceId = httpContext.TraceIdentifier;

        var (statusCode, message) = exception switch
        {
            InvalidOperationException ex => (StatusCodes.Status500InternalServerError, ex.Message),
            ArgumentException ex => (StatusCodes.Status400BadRequest, ex.Message),
            KeyNotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "เกิดข้อผิดพลาดภายในเซิร์ฟเวอร์"),
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(ApiResponse.Fail(message, traceId), Json, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
