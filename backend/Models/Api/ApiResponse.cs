namespace backend.Models.Api;

/// <summary>รูปแบบ response มาตรฐานของ API</summary>
public sealed class ApiResponse
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public string TraceId { get; init; } = string.Empty;

    public object? Data { get; init; }

    public static ApiResponse Ok(object? data, string traceId, string message = "success") =>
        new()
        {
            Success = true,
            Message = message,
            TraceId = traceId,
            Data = data,
        };

    public static ApiResponse Fail(string message, string traceId) =>
        new()
        {
            Success = false,
            Message = message,
            TraceId = traceId,
            Data = null,
        };
}
