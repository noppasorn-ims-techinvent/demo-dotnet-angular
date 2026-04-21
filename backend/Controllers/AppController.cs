using backend.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class AppController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AppController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("config")]
    public ActionResult<AppConfigDto> GetConfig()
    {
        return Ok(new AppConfigDto
        {
            ApiBaseUrl = _configuration["PublicApiBaseUrl"] ?? string.Empty,
            AppName = _configuration["AppName"] ?? "Marketplace Demo",
        });
    }
}
