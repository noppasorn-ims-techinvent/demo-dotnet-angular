using System.Reflection;
using backend.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;

namespace backend.Filters;

/// <summary>
/// ห่อทุก JSON response เป็น <see cref="ApiResponse"/> (ยกเว้น 204 และผลลัพธ์ที่ไม่ใช่ ObjectResult)
/// </summary>
public sealed class ApiResponseEnvelopeFilter : IAsyncAlwaysRunResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        var traceId = context.HttpContext.TraceIdentifier;

        switch (context.Result)
        {
            case ObjectResult obj:
                ApplyToObjectResult(obj, traceId);
                break;
            case NotFoundResult:
                context.Result = new ObjectResult(ApiResponse.Fail("Not found", traceId))
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    DeclaredType = typeof(ApiResponse),
                };
                break;
            case UnauthorizedResult:
                context.Result = new ObjectResult(ApiResponse.Fail("Unauthorized", traceId))
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    DeclaredType = typeof(ApiResponse),
                };
                break;
            case ForbidResult:
                context.Result = new ObjectResult(ApiResponse.Fail("Forbidden", traceId))
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                    DeclaredType = typeof(ApiResponse),
                };
                break;
            case NoContentResult:
                break;
            default:
                break;
        }

        return next();
    }

    private static void ApplyToObjectResult(ObjectResult obj, string traceId)
    {
        if (obj.Value is ApiResponse)
        {
            return;
        }

        if (obj.Value is FileResult or PhysicalFileResult or VirtualFileResult)
        {
            return;
        }

        var status = obj.StatusCode ?? StatusCodes.Status200OK;
        if (status == StatusCodes.Status204NoContent)
        {
            return;
        }

        if (status is >= 200 and < 300)
        {
            obj.Value = ApiResponse.Ok(obj.Value, "success", traceId);
            obj.DeclaredType = typeof(ApiResponse);
            return;
        }

        if (status is >= 400)
        {
            var msg = ExtractClientMessage(obj.Value) ?? DefaultMessageForStatus(status);
            obj.Value = ApiResponse.Fail(msg, traceId);
            obj.DeclaredType = typeof(ApiResponse);
        }
    }

    private static string? ExtractClientMessage(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is ProblemDetails pd)
        {
            if (!string.IsNullOrWhiteSpace(pd.Detail))
            {
                return pd.Detail;
            }

            if (!string.IsNullOrWhiteSpace(pd.Title))
            {
                return pd.Title;
            }
        }

        if (value is ValidationProblemDetails vpd)
        {
            if (!string.IsNullOrWhiteSpace(vpd.Detail))
            {
                return vpd.Detail;
            }

            if (vpd.Errors.Count > 0)
            {
                var first = vpd.Errors.First();
                return $"{first.Key}: {string.Join("; ", first.Value)}";
            }
        }

        var prop = value.GetType().GetProperty(
            "message",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop?.GetValue(value) is string s && !string.IsNullOrWhiteSpace(s))
        {
            return s;
        }

        return null;
    }

    private static string DefaultMessageForStatus(int status) =>
        status switch
        {
            StatusCodes.Status400BadRequest => "Bad request",
            StatusCodes.Status401Unauthorized => "Unauthorized",
            StatusCodes.Status403Forbidden => "Forbidden",
            StatusCodes.Status404NotFound => "Not found",
            StatusCodes.Status409Conflict => "Conflict",
            _ => ReasonPhrases.GetReasonPhrase(status),
        };
}
