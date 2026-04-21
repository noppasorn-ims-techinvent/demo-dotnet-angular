using backend.Data;
using backend.Models.Dtos;
using backend.Models.Entities;
using backend.Models.Enums;
using backend.Hubs;
using backend.Queue;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly IOrderRepository _orders;
    private readonly IHubContext<MarketplaceHub> _hub;
    private readonly IOrderPlacedQueue _orderQueue;

    public OrderService(
        AppDbContext db,
        IOrderRepository orders,
        IHubContext<MarketplaceHub> hub,
        IOrderPlacedQueue orderQueue)
    {
        _db = db;
        _orders = orders;
        _hub = hub;
        _orderQueue = orderQueue;
    }

    public async Task<OrderDto?> PlaceOrderAsync(int buyerId, PlaceOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0)
        {
            return null;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var order = new Order
            {
                BuyerId = buyerId,
                Status = OrderStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow,
                Lines = new List<OrderLine>(),
            };

            decimal total = 0;
            var affectedSellerIds = new HashSet<int>();

            foreach (var line in request.Lines)
            {
                var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == line.ProductId, cancellationToken);
                if (product is null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return null;
                }

                if (product.StockQuantity < line.Quantity)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw new InvalidOperationException($"Insufficient stock for product '{product.Name}'.");
                }

                affectedSellerIds.Add(product.SellerId);
                product.StockQuantity -= line.Quantity;
                var lineTotal = product.Price * line.Quantity;
                total += lineTotal;
                order.Lines.Add(new OrderLine
                {
                    ProductId = product.Id,
                    Quantity = line.Quantity,
                    UnitPrice = product.Price,
                });
            }

            order.TotalAmount = total;
            await _db.Orders.AddAsync(order, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            foreach (var sellerId in affectedSellerIds)
            {
                await _hub.Clients.Group(MarketplaceHub.SellerGroupName(sellerId))
                    .SendAsync(
                        "sellerNewOrder",
                        new
                        {
                            orderId = order.Id,
                            buyerId,
                            totalAmount = order.TotalAmount,
                            lineCount = order.Lines.Count,
                        },
                        cancellationToken);
            }

            await _orderQueue.EnqueueAsync(new OrderPlacedMessage(order.Id, buyerId, order.TotalAmount), cancellationToken);

            var dto = await _orders.GetByIdWithLinesAsync(order.Id, cancellationToken);
            return dto is null ? null : Map(dto);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(int buyerId, CancellationToken cancellationToken = default)
    {
        var list = await _orders.GetByBuyerIdAsync(buyerId, cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<OrderDto?> GetOrderAsync(int userId, IReadOnlySet<string> roles, int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdWithLinesAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        if (order.BuyerId != userId && !roles.Contains("Admin"))
        {
            return null;
        }

        return Map(order);
    }

    public async Task<IReadOnlyList<OrderDto>> GetAllOrdersAsync(CancellationToken cancellationToken = default)
    {
        var list = await _orders.GetAllAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<OrderDto?> UpdateStatusAsync(int orderId, OrderStatus status, CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        order.Status = status;
        await _db.SaveChangesAsync(cancellationToken);

        await _hub.Clients.Group(MarketplaceHub.BuyerGroupName(order.BuyerId))
            .SendAsync("orderStatusChanged", new { orderId = order.Id, status = order.Status.ToString() }, cancellationToken);

        var full = await _orders.GetByIdWithLinesAsync(order.Id, cancellationToken);
        return full is null ? null : Map(full);
    }

    public async Task<bool> DeleteOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var order = await _db.Orders
                .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order is null)
            {
                return false;
            }

            var buyerId = order.BuyerId;
            var removedOrderId = order.Id;
            var affectedSellerIds = order.Lines
                .Where(l => l.Product is not null)
                .Select(l => l.Product!.SellerId)
                .Distinct()
                .ToList();

            foreach (var line in order.Lines)
            {
                var product = line.Product ?? await _db.Products.FirstOrDefaultAsync(p => p.Id == line.ProductId, cancellationToken);
                if (product is not null)
                {
                    product.StockQuantity += line.Quantity;
                }
            }

            _db.Orders.Remove(order);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await _hub.Clients.Group(MarketplaceHub.BuyerGroupName(buyerId))
                .SendAsync("buyerOrderDeletedByAdmin", new { orderId = removedOrderId }, cancellationToken);

            foreach (var sellerId in affectedSellerIds)
            {
                await _hub.Clients.Group(MarketplaceHub.SellerGroupName(sellerId))
                    .SendAsync("sellerOrderRemovedByAdmin", new { orderId = removedOrderId }, cancellationToken);
            }

            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static OrderDto Map(Order o)
    {
        return new OrderDto
        {
            Id = o.Id,
            BuyerId = o.BuyerId,
            Status = o.Status,
            TotalAmount = o.TotalAmount,
            CreatedAtUtc = o.CreatedAtUtc,
            Lines = o.Lines.Select(l => new OrderLineDto
            {
                ProductId = l.ProductId,
                ProductName = l.Product?.Name ?? string.Empty,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
            }).ToList(),
        };
    }
}
