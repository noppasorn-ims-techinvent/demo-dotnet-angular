using backend.Utilities.Interface;

namespace backend.DTO;

public class Result<T>
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string TraceId { get; }

    public T? Data { get; set; }

    public Result(ITrace trace)
    {
        TraceId = trace.GetTraceId();
    }

    public string GetLog()
    {
        return $"Success: [{Success}] Message: [{Message}] TraceId: [{TraceId}] Data: [{Data}]";
    }

    public string GetLogWithNoData()
    {
        return $"Success: [{Success}] Message: [{Message}] TraceId: [{TraceId}]";
    }
}
