namespace backend.Models.Responses;

/// <summary>รูปแบบ response มาตรฐานของ API</summary>
public sealed class ApiResponse
{
    public bool Success { get; init; }

    public string Message { get; init; } = "success";

    public string TraceId { get; init; } = string.Empty;

    public object? Data { get; init; }

    public static ApiResponse Ok(object? data, string message = "success", string traceId = "") =>
        new()
        {
            Success = true,
            Message = message,
            TraceId = traceId,
            Data = data,
        };

    public static ApiResponse Fail(string message, string traceId, object? data = null) =>
        new()
        {
            Success = false,
            Message = message,
            TraceId = traceId,
            Data = data,
        };
}
