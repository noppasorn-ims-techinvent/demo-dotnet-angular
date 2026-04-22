using System.IdentityModel.Tokens.Jwt;
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
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    private readonly AppSettings _appSettings;
    private readonly ITrace _trace;

    public OrdersController(IOrderService orders, AppSettings appSettings, ITrace trace)
    {
        _orders = orders;
        _appSettings = appSettings;
        _trace = trace;
    }

    [HttpPost]
    [Authorize(Roles = "Buyer")]
    public async Task<Result<OrderDto>> PlaceOrderAsync([FromBody] PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        Result<OrderDto> result = new(_trace);
        var buyerId = GetUserId();
        if (buyerId is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.Unauthorized;
            return result;
        }

        var order = await _orders.PlaceOrderAsync(buyerId.Value, request, cancellationToken);
        if (order is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.InvalidOrderLines;
            return result;
        }

        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = order;
        return result;
    }

    [HttpGet]
    [Authorize(Roles = "Buyer")]
    public async Task<Result<IReadOnlyList<OrderDto>>> GetMineAsync(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<OrderDto>> result = new(_trace);
        var buyerId = GetUserId();
        if (buyerId is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.Unauthorized;
            return result;
        }

        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = await _orders.GetMyOrdersAsync(buyerId.Value, cancellationToken);
        return result;
    }

    [HttpGet]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<Result<IReadOnlyList<OrderDto>>> GetForMyStoreAsync(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<OrderDto>> result = new(_trace);
        var sellerId = GetUserId();
        if (sellerId is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.Unauthorized;
            return result;
        }

        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = await _orders.GetOrdersForMyStoreAsync(sellerId.Value, cancellationToken);
        return result;
    }

    [HttpGet("{id:int}", Name = "GetOrderById")]
    [Authorize]
    public async Task<Result<OrderDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<OrderDto> result = new(_trace);
        var userId = GetUserId();
        if (userId is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.Unauthorized;
            return result;
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
        var order = await _orders.GetOrderAsync(userId.Value, roles, id, cancellationToken);
        if (order is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.NotFound;
            return result;
        }

        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = order;
        return result;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<Result<IReadOnlyList<OrderDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var list = await _orders.GetAllOrdersAsync(cancellationToken);
        return new Result<IReadOnlyList<OrderDto>>(_trace)
        {
            Success = true,
            Message = _appSettings.SuccessMessage.Success,
            Data = list,
        };
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<Result<OrderDto>> UpdateStatusAsync(int id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        Result<OrderDto> result = new(_trace);
        var updated = await _orders.UpdateStatusAsync(id, request.Status, cancellationToken);
        if (updated is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.NotFound;
            return result;
        }

        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = updated;
        return result;
    }

    [HttpPost("{id:int}/simulate-payment")]
    [Authorize(Roles = "Buyer")]
    public async Task<Result<OrderDto>> SimulatePaymentAsync(
        int id,
        [FromBody] SimulatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        Result<OrderDto> result = new(_trace);
        var buyerId = GetUserId();
        if (buyerId is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.Unauthorized;
            return result;
        }

        var dto = await _orders.SimulatePaymentAsync(buyerId.Value, id, request, cancellationToken);
        if (dto is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.NotFound;
            return result;
        }

        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = dto;
        return result;
    }

    [HttpPost("{id:int}/request-cancellation")]
    [Authorize(Roles = "Buyer")]
    public async Task<Result<OrderDto>> RequestCancellationAsync(
        int id,
        [FromBody] RequestCancellationRequest request,
        CancellationToken cancellationToken)
    {
        Result<OrderDto> result = new(_trace);
        var buyerId = GetUserId();
        if (buyerId is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.Unauthorized;
            return result;
        }

        var dto = await _orders.RequestCancellationAsync(buyerId.Value, id, request, cancellationToken);
        if (dto is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.NotFound;
            return result;
        }

        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = dto;
        return result;
    }

    [HttpPost("{id:int}/review-cancellation")]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<Result<OrderDto>> ReviewCancellationAsync(
        int id,
        [FromBody] ReviewCancellationRequest request,
        [FromQuery] int? targetSellerUserId,
        CancellationToken cancellationToken)
    {
        Result<OrderDto> result = new(_trace);
        var userId = GetUserId();
        if (userId is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.Unauthorized;
            return result;
        }

        if (request.TargetSellerUserId is null or < 1 && targetSellerUserId is >= 1)
        {
            request.TargetSellerUserId = targetSellerUserId;
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
        var dto = await _orders.ReviewCancellationAsync(userId.Value, roles, id, request, cancellationToken);
        if (dto is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.NotFound;
            return result;
        }

        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = dto;
        return result;
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<Result<object>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        Result<object> result = new(_trace);
        var deleted = await _orders.DeleteOrderAsync(id, cancellationToken);
        if (!deleted)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.NotFound;
            return result;
        }

        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = null;
        return result;
    }

    private int? GetUserId()
    {
        var v = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");
        return int.TryParse(v, out var uid) ? uid : null;
    }
}
