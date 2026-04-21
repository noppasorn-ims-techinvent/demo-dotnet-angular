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
        var updated = await _orders.UpdateStatusAsync(id, request.Status, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
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
        var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(v, out var uid) ? uid : null;
    }
}
