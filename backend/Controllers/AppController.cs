using backend.DTO;
using backend.Models.Dtos;
using backend.Utilities;
using backend.Utilities.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[Route(Constant.AuthorizeConfig.RouteController)]
[ApiController]
[AllowAnonymous]
public class AppController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AppSettings _appSettings;
    private readonly ITrace _trace;

    public AppController(IConfiguration configuration, AppSettings appSettings, ITrace trace)
    {
        _configuration = configuration;
        _appSettings = appSettings;
        _trace = trace;
    }

    [HttpGet]
    public Result<AppConfigDto> GetConfig()
    {
        Result<AppConfigDto> result = new(_trace)
        {
            Success = true,
            Message = _appSettings.SuccessMessage.Success,
            Data = new AppConfigDto
            {
                ApiBaseUrl = _configuration["PublicApiBaseUrl"] ?? string.Empty,
                AppName = _configuration["AppName"] ?? "Marketplace Demo",
            },
        };
        return result;
    }
}
