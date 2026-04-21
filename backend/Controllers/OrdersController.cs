using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using backend.Models.Dtos;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

public class OrdersController : BaseApiController
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders)
    {
        _orders = orders;
    }

    [HttpPost]
    [Authorize(Roles = "Buyer")]
    public async Task<ActionResult<OrderDto>> PlaceOrderAsync([FromBody] PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var buyerId = GetUserId();
        if (buyerId is null)
        {
            return Unauthorized();
        }

        try
        {
            var order = await _orders.PlaceOrderAsync(buyerId.Value, request, cancellationToken);
            if (order is null)
            {
                return BadRequest(new { message = "Invalid order lines." });
            }

            return CreatedAtRoute("GetOrderById", new { id = order.Id }, order);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Buyer")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetMineAsync(CancellationToken cancellationToken)
    {
        var buyerId = GetUserId();
        if (buyerId is null)
        {
            return Unauthorized();
        }

        return Ok(await _orders.GetMyOrdersAsync(buyerId.Value, cancellationToken));
    }

    /// <summary>คำสั่งซื้อที่มีสินค้าของร้านนี้ (ผู้ขาย / แอดมินที่ลงสินค้าเอง)</summary>
    [HttpGet("for-store")]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetForMyStoreAsync(CancellationToken cancellationToken)
    {
        var sellerId = GetUserId();
        if (sellerId is null)
        {
            return Unauthorized();
        }

        return Ok(await _orders.GetOrdersForMyStoreAsync(sellerId.Value, cancellationToken));
    }

    [HttpGet("{id:int}", Name = "GetOrderById")]
    [Authorize]
    public async Task<ActionResult<OrderDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
        var order = await _orders.GetOrderAsync(userId.Value, roles, id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IReadOnlyList<OrderDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Ok(await _orders.GetAllOrdersAsync(cancellationToken));
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderDto>> UpdateStatusAsync(int id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _orders.UpdateStatusAsync(id, request.Status, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/simulate-payment")]
    [Authorize(Roles = "Buyer")]
    public async Task<ActionResult<OrderDto>> SimulatePaymentAsync(
        int id,
        [FromBody] SimulatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var buyerId = GetUserId();
        if (buyerId is null)
        {
            return Unauthorized();
        }

        try
        {
            var dto = await _orders.SimulatePaymentAsync(buyerId.Value, id, request, cancellationToken);
            return dto is null ? NotFound() : Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/request-cancellation")]
    [Authorize(Roles = "Buyer")]
    public async Task<ActionResult<OrderDto>> RequestCancellationAsync(
        int id,
        [FromBody] RequestCancellationRequest request,
        CancellationToken cancellationToken)
    {
        var buyerId = GetUserId();
        if (buyerId is null)
        {
            return Unauthorized();
        }

        try
        {
            var dto = await _orders.RequestCancellationAsync(buyerId.Value, id, request, cancellationToken);
            return dto is null ? NotFound() : Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/review-cancellation")]
    [Authorize(Roles = "Admin,Seller")]
    public async Task<ActionResult<OrderDto>> ReviewCancellationAsync(
        int id,
        [FromBody] ReviewCancellationRequest request,
        [FromQuery] int? targetSellerUserId,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        if (request.TargetSellerUserId is null or < 1 && targetSellerUserId is >= 1)
        {
            request.TargetSellerUserId = targetSellerUserId;
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
        try
        {
            var dto = await _orders.ReviewCancellationAsync(userId.Value, roles, id, request, cancellationToken);
            return dto is null ? NotFound() : Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var deleted = await _orders.DeleteOrderAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private int? GetUserId()
    {
        var v = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue("sub");
        return int.TryParse(v, out var uid) ? uid : null;
    }
}
