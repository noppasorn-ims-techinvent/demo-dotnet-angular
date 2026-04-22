using System.Security.Claims;
using backend.DTO;
using backend.Models.Dtos;
using backend.Services.Interfaces;
using backend.Utilities;
using backend.Utilities.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route(Constant.AuthorizeConfig.RouteController)]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly AppSettings _appSettings;
    private readonly ITrace _trace;

    public AuthController(IAuthService auth, AppSettings appSettings, ITrace trace)
    {
        _auth = auth;
        _appSettings = appSettings;
        _trace = trace;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<Result<AuthResponse>> RegisterAsync([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var login = await _auth.RegisterAsync(request, cancellationToken);
        Result<AuthResponse> result = new(_trace);
        if (login is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.EmailAlreadyRegistered;
            return result;
        }

        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = login;
        return result;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<Result<AuthResponse>> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var login = await _auth.LoginAsync(request, cancellationToken);
        Result<AuthResponse> result = new(_trace);
        if (login is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.InvalidCredentials;
            return result;
        }

        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = login;
        return result;
    }

    [HttpGet]
    [Authorize]
    public async Task<Result<UserDto>> MeAsync(CancellationToken cancellationToken)
    {
        Result<UserDto> result = new(_trace);
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idClaim, out var userId))
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.Unauthorized;
            return result;
        }

        var user = await _auth.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.NotFound;
            return result;
        }

        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = user;
        return result;
    }
}
