using System.Reflection;
using backend.Models.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace backend.Filters;

/// <summary>
/// ห่อทุก ObjectResult / CreatedAtRoute และ status ว่าง body บางแบบ ให้เป็น <see cref="ApiResponse"/>
/// </summary>
public sealed class ApiEnvelopeResultFilter : IAsyncAlwaysRunResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        WrapIfNeeded(context);
        return next();
    }

    private static void WrapIfNeeded(ResultExecutingContext context)
    {
        var http = context.HttpContext;
        if (http.Request.Path.StartsWithSegments("/swagger"))
        {
            return;
        }

        var traceId = http.TraceIdentifier;
        var result = context.Result;
        if (result is null)
        {
            return;
        }

        switch (result)
        {
            case CreatedAtRouteResult cr:
                if (!IsEnvelope(cr.Value))
                {
                    cr.Value = ApiResponse.Ok(cr.Value, traceId);
                }

                return;

            case ObjectResult obj:
                TransformObjectResult(obj, traceId);
                return;

            case NoContentResult:
                // HTTP 204 ต้องไม่มี body — ไม่ห่อ
                return;

            case NotFoundResult:
                context.Result = new ObjectResult(ApiResponse.Fail("Not found", traceId)) { StatusCode = StatusCodes.Status404NotFound };
                return;

            case UnauthorizedResult:
                context.Result = new ObjectResult(ApiResponse.Fail("Unauthorized", traceId)) { StatusCode = StatusCodes.Status401Unauthorized };
                return;

            case ForbidResult:
                context.Result = new ObjectResult(ApiResponse.Fail("Forbidden", traceId)) { StatusCode = StatusCodes.Status403Forbidden };
                return;

            default:
                return;
        }
    }

    private static void TransformObjectResult(ObjectResult obj, string traceId)
    {
        if (IsEnvelope(obj.Value))
        {
            return;
        }

        var code = obj.StatusCode ?? StatusCodes.Status200OK;

        if (code is >= 200 and < 300)
        {
            obj.Value = ApiResponse.Ok(obj.Value, traceId);
            return;
        }

        if (code is >= 400)
        {
            var msg = ExtractMessage(obj.Value) ?? ReasonForStatusCode(code);
            obj.Value = ApiResponse.Fail(msg, traceId);
        }
    }

    private static bool IsEnvelope(object? value) => value is ApiResponse;

    private static string? ExtractMessage(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is ProblemDetails pd)
        {
            return pd.Detail ?? pd.Title;
        }

        if (value is string s)
        {
            return s;
        }

        foreach (var name in new[] { "message", "Message", "title", "Title" })
        {
            var prop = value.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop?.GetValue(value) is string str && !string.IsNullOrWhiteSpace(str))
            {
                return str;
            }
        }

        return null;
    }

    private static string ReasonForStatusCode(int code) =>
        code switch
        {
            StatusCodes.Status400BadRequest => "Bad request",
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "Not found",
            StatusCodes.Status409Conflict => "Conflict",
            _ => "Error",
        };
}
