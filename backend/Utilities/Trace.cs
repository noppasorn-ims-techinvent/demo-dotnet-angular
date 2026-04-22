using System.Diagnostics;
using backend.Utilities.Interface;
using Microsoft.AspNetCore.Http;

namespace backend.Utilities;

public sealed class Trace : ITrace
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Trace(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetTraceId()
    {
        string errorNumber = Activity.Current?.Id ?? _httpContextAccessor.HttpContext?.TraceIdentifier!;
        return errorNumber;
    }
}
