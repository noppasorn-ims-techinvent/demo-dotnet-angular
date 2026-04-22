using System.Net;
using backend.DTO;
using backend.Utilities;
using backend.Utilities.Interface;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ErrorController : ControllerBase
{
    [Route("/error")]
    public Result<object> Error([FromServices] ITrace trace, [FromServices] AppSettings appSettings)
    {
        var result = new Result<object>(trace)
        {
            Success = false,
            Message = appSettings.ErrorMessage.General,
        };

        HttpContext.Response.StatusCode = (int)HttpStatusCode.OK;
        return result;
    }
}
