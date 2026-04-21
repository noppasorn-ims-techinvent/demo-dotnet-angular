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

    public async Task<IReadOnlyList<OrderDto>> GetOrdersForMyStoreAsync(int sellerUserId, CancellationToken cancellationToken = default)
    {
        var list = await _orders.GetBySellerUserIdAsync(sellerUserId, cancellationToken);
        return list.Select(o => MapForSeller(o, sellerUserId)).ToList();
    }

    public async Task<OrderDto?> GetOrderAsync(int userId, IReadOnlySet<string> roles, int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdWithLinesAsync(orderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        if (roles.Contains("Admin"))
        {
            return Map(order);
        }

        if (order.BuyerId == userId)
        {
            return Map(order);
        }

        if (roles.Contains("Seller") && order.Lines.Any(l => l.Product?.SellerId == userId))
        {
            return MapForSeller(order, userId);
        }

        return null;
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

        if (order.Status == OrderStatus.CancellationPending)
        {
            throw new InvalidOperationException(
                "คำสั่งซื้อนี้อยู่ระหว่างรอยกเลิก ให้ใช้การพิจารณาอนุมัติ/ไม่อนุมัติคำขอยกเลิกแทนการเปลี่ยนสถานะจากเมนูนี้");
        }

        if (status == OrderStatus.CancellationPending)
        {
            throw new InvalidOperationException(
                "สถานะรอยกเลิกเกิดจากคำขอของลูกค้าเท่านั้น ไม่สามารถตั้งจากเมนูนี้");
        }

        order.Status = status;
        await _db.SaveChangesAsync(cancellationToken);

        var full = await _orders.GetByIdWithLinesAsync(order.Id, cancellationToken);
        if (full is not null)
        {
            await BroadcastOrderStatusAsync(full, cancellationToken);
        }

        return full is null ? null : Map(full);
    }

    public async Task<OrderDto?> SimulatePaymentAsync(
        int buyerId,
        int orderId,
        SimulatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateSimulatePayment(request);
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.BuyerId == buyerId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        if (order.Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException("ชำระเงินจำลองได้เฉพาะคำสั่งซื้อที่สถานะ «รอชำระเงิน» เท่านั้น");
        }

        order.Status = OrderStatus.Paid;
        order.SimulatedPaymentMethod = request.PaymentMethod.Trim().ToLowerInvariant();
        await _db.SaveChangesAsync(cancellationToken);

        var full = await _orders.GetByIdWithLinesAsync(order.Id, cancellationToken);
        if (full is null)
        {
            return null;
        }

        await BroadcastOrderStatusAsync(full, cancellationToken);
        return Map(full);
    }

    public async Task<OrderDto?> RequestCancellationAsync(
        int buyerId,
        int orderId,
        RequestCancellationRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.BuyerId == buyerId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        if (order.Status == OrderStatus.CancellationPending)
        {
            throw new InvalidOperationException("ส่งคำขอยกเลิกแล้ว — รอร้านค้าหรือผู้ดูแลระบบพิจารณา");
        }

        if (order.Status is not (OrderStatus.Pending or OrderStatus.Paid))
        {
            throw new InvalidOperationException("ขอยกเลิกได้เฉพาะคำสั่งซื้อที่รอชำระหรือชำระแล้ว (ยังไม่จัดส่ง)");
        }

        order.PreCancellationStatus = order.Status;
        order.Status = OrderStatus.CancellationPending;
        order.BuyerCancellationReason = request.Reason.Trim();
        order.CancellationReviewerNote = null;
        order.CancellationReviewedByUserId = null;
        await _db.SaveChangesAsync(cancellationToken);

        var full = await _orders.GetByIdWithLinesAsync(order.Id, cancellationToken);
        if (full is not null)
        {
            await BroadcastCancellationHubAsync(full, approved: null, cancellationToken);
        }

        return full is null ? null : Map(full);
    }

    public async Task<OrderDto?> ReviewCancellationAsync(
        int reviewerUserId,
        IReadOnlySet<string> roles,
        int orderId,
        ReviewCancellationRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders
            .Include(o => o.Lines)
            .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null || order.Status != OrderStatus.CancellationPending)
        {
            return null;
        }

        if (!roles.Contains("Admin"))
        {
            if (!roles.Contains("Seller"))
            {
                return null;
            }

            if (!order.Lines.Any(l => l.Product?.SellerId == reviewerUserId))
            {
                return null;
            }
        }

        if (request.Approved)
        {
            foreach (var line in order.Lines)
            {
                var product = line.Product ?? await _db.Products.FirstOrDefaultAsync(p => p.Id == line.ProductId, cancellationToken);
                if (product is not null)
                {
                    product.StockQuantity += line.Quantity;
                }
            }

            order.Status = OrderStatus.Cancelled;
            order.PreCancellationStatus = null;
            order.CancellationReviewerNote = request.Note.Trim();
            order.CancellationReviewedByUserId = reviewerUserId;
        }
        else
        {
            order.Status = order.PreCancellationStatus ?? OrderStatus.Paid;
            order.PreCancellationStatus = null;
            order.CancellationReviewerNote = request.Note.Trim();
            order.CancellationReviewedByUserId = reviewerUserId;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var full = await _orders.GetByIdWithLinesAsync(order.Id, cancellationToken);
        if (full is null)
        {
            return null;
        }

        await BroadcastOrderStatusAsync(full, cancellationToken);
        await BroadcastCancellationHubAsync(full, (bool?)request.Approved, cancellationToken);
        return Map(full);
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

    private async Task BroadcastOrderStatusAsync(Order full, CancellationToken cancellationToken)
    {
        await _hub.Clients.Group(MarketplaceHub.BuyerGroupName(full.BuyerId))
            .SendAsync("orderStatusChanged", new { orderId = full.Id, status = full.Status.ToString() }, cancellationToken);
        foreach (var sellerId in full.Lines.Where(l => l.Product is not null).Select(l => l.Product!.SellerId).Distinct())
        {
            await _hub.Clients.Group(MarketplaceHub.SellerGroupName(sellerId))
                .SendAsync("storeOrderStatusChanged", new { orderId = full.Id, status = full.Status.ToString() }, cancellationToken);
        }
    }

    /// <param name="approved">null = มีคำขอยกเลิกใหม่รอพิจารณา</param>
    private async Task BroadcastCancellationHubAsync(Order full, bool? approved, CancellationToken cancellationToken)
    {
        var payload = new
        {
            orderId = full.Id,
            buyerId = full.BuyerId,
            kind = approved is null ? "pending" : "reviewed",
            approved,
        };

        await _hub.Clients.Group(MarketplaceHub.BuyerGroupName(full.BuyerId)).SendAsync("orderCancellationUpdated", payload, cancellationToken);
        await _hub.Clients.Group(MarketplaceHub.AdminsGroup).SendAsync("orderCancellationUpdated", payload, cancellationToken);
        foreach (var sellerId in full.Lines.Where(l => l.Product is not null).Select(l => l.Product!.SellerId).Distinct())
        {
            await _hub.Clients.Group(MarketplaceHub.SellerGroupName(sellerId)).SendAsync("orderCancellationUpdated", payload, cancellationToken);
        }
    }

    private static void ValidateSimulatePayment(SimulatePaymentRequest req)
    {
        var method = req.PaymentMethod.Trim().ToLowerInvariant();
        if (method is not ("card" or "bank"))
        {
            throw new InvalidOperationException("เลือกวิธีชำระ: บัตรเครดิต (card) หรือโอนธนาคาร (bank)");
        }

        if (method == "card")
        {
            var digits = (req.CardNumber ?? string.Empty).Where(char.IsDigit).ToArray();
            if (digits.Length is < 12 or > 19)
            {
                throw new InvalidOperationException("หมายเลขบัตร (จำลอง) ให้กรอกตัวเลข 12–19 หลัก");
            }

            if (string.IsNullOrWhiteSpace(req.CardHolder) || req.CardHolder.Trim().Length < 2)
            {
                throw new InvalidOperationException("กรุณากรอกชื่อบนบัตร");
            }

            var exp = (req.CardExpiry ?? string.Empty).Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(exp, @"^\d{2}/\d{2}$"))
            {
                throw new InvalidOperationException("วันหมดอายุบัตร รูปแบบ ดด/ปป (เช่น 12/28)");
            }

            var cvvDigits = (req.CardCvv ?? string.Empty).Where(char.IsDigit).ToArray();
            if (cvvDigits.Length is < 3 or > 4)
            {
                throw new InvalidOperationException("CVV ตัวเลข 3–4 หลัก");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(req.BankCode))
            {
                throw new InvalidOperationException("เลือกธนาคาร");
            }

            var acct = (req.BankAccountNumber ?? string.Empty).Where(char.IsDigit).ToArray();
            if (acct.Length is < 8 or > 15)
            {
                throw new InvalidOperationException("เลขบัญชี (จำลอง) ตัวเลข 8–15 หลัก");
            }
        }
    }

    private static OrderDto Map(Order o)
    {
        var dto = new OrderDto
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
        CopyLifecycle(o, dto);
        return dto;
    }

    private static void CopyLifecycle(Order o, OrderDto dto)
    {
        dto.PreCancellationStatus = o.PreCancellationStatus;
        dto.BuyerCancellationReason = o.BuyerCancellationReason;
        dto.CancellationReviewerNote = o.CancellationReviewerNote;
        dto.CancellationReviewedByUserId = o.CancellationReviewedByUserId;
        dto.SimulatedPaymentMethod = o.SimulatedPaymentMethod;
    }

    /// <summary>มุมมองร้าน:เฉพาะบรรทัดที่ Product.SellerId ตรงกับร้าน — TotalAmount เป็นยอดรวมเฉพาะบรรทัดเหล่านั้น</summary>
    private static OrderDto MapForSeller(Order o, int sellerUserId)
    {
        var lines = o.Lines
            .Where(l => l.Product?.SellerId == sellerUserId)
            .Select(l => new OrderLineDto
            {
                ProductId = l.ProductId,
                ProductName = l.Product?.Name ?? string.Empty,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
            })
            .ToList();
        var subtotal = lines.Sum(l => l.UnitPrice * l.Quantity);
        var dto = new OrderDto
        {
            Id = o.Id,
            BuyerId = o.BuyerId,
            Status = o.Status,
            TotalAmount = subtotal,
            CreatedAtUtc = o.CreatedAtUtc,
            Lines = lines,
        };
        CopyLifecycle(o, dto);
        return dto;
    }
}
