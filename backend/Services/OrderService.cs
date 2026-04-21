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

            await NotifySellersNewOrderAsync(order.Id, buyerId, order.TotalAmount, order.Lines.Count, affectedSellerIds, cancellationToken);

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
        var order = await _db.Orders
            .Include(o => o.Lines)
            .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.BuyerId == buyerId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        if (order.Status == OrderStatus.CancellationPending)
        {
            throw new InvalidOperationException("ส่งคำขอยกเลิกแล้ว — รอแต่ละร้านพิจารณาแยกกัน");
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
        foreach (var line in order.Lines)
        {
            line.LineCancellationState = OrderLineCancellationState.PendingSellerDecision;
        }

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
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        Order? splitChild = null;
        try
        {
            var order = await _db.Orders
                .Include(o => o.Lines)
                .ThenInclude(l => l.Product)
                .ThenInclude(p => p.Seller)
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order is null || order.Status != OrderStatus.CancellationPending)
            {
                return null;
            }

            if (!roles.Contains("Admin") && !order.Lines.Any(l => l.Product?.SellerId == reviewerUserId))
            {
                return null;
            }

            var sellerUserIdsForFinalNotify = order.Lines
                .Where(l => l.Product is not null)
                .Select(l => l.Product!.SellerId)
                .Distinct()
                .ToList();

            int targetSellerId;
            var explicitTarget = request.TargetSellerUserId;
            if (explicitTarget is >= 1)
            {
                if (roles.Contains("Admin"))
                {
                    targetSellerId = explicitTarget.Value;
                }
                else if (roles.Contains("Seller") && explicitTarget.Value == reviewerUserId)
                {
                    targetSellerId = explicitTarget.Value;
                }
                else
                {
                    return null;
                }
            }
            else if (roles.Contains("Seller"))
            {
                targetSellerId = reviewerUserId;
            }
            else if (roles.Contains("Admin"))
            {
                // แอดมินอย่างเดียว (ไม่มี role Seller) แต่ใช้หน้าร้านค้า — สินค้าเป็น seller id = user ตัวเอง
                if (order.Lines.Any(l =>
                        l.Product?.SellerId == reviewerUserId
                        && LineRequiresSellerCancellationReview(l, order.Status)))
                {
                    targetSellerId = reviewerUserId;
                }
                else
                {
                    var pendingSellers = order.Lines
                        .Where(l => l.Product is not null && LineRequiresSellerCancellationReview(l, order.Status))
                        .Select(l => l.Product!.SellerId)
                        .Distinct()
                        .ToList();

                    if (pendingSellers.Count == 1)
                    {
                        targetSellerId = pendingSellers[0];
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "ผู้ดูแลระบบต้องระบุ TargetSellerUserId (รหัสผู้ขายของร้านที่กำลังพิจารณา) แยกตามร้าน");
                    }
                }
            }
            else
            {
                return null;
            }

            var targetLines = order.Lines
                .Where(l =>
                    l.Product?.SellerId == targetSellerId
                    && LineRequiresSellerCancellationReview(l, order.Status))
                .ToList();

            if (targetLines.Count == 0)
            {
                throw new InvalidOperationException("ไม่มีรายการของร้านนี้ที่รอพิจารณายกเลิก");
            }

            var noteTrim = request.Note.Trim();
            AppendSellerCancellationNote(order, targetSellerId, noteTrim);
            order.CancellationReviewedByUserId = reviewerUserId;

            var amountDelta = targetLines.Sum(l => l.UnitPrice * l.Quantity);

            if (request.Approved)
            {
                foreach (var line in targetLines)
                {
                    var product = line.Product ?? await _db.Products.FirstOrDefaultAsync(p => p.Id == line.ProductId, cancellationToken);
                    if (product is not null)
                    {
                        product.StockQuantity += line.Quantity;
                    }

                    _db.OrderLines.Remove(line);
                }

                order.TotalAmount -= amountDelta;
                if (order.TotalAmount < 0)
                {
                    order.TotalAmount = 0;
                }
            }
            else
            {
                splitChild = new Order
                {
                    BuyerId = order.BuyerId,
                    Status = order.PreCancellationStatus ?? OrderStatus.Paid,
                    CreatedAtUtc = DateTime.UtcNow,
                    TotalAmount = amountDelta,
                    SplitSourceOrderId = order.Id,
                    SimulatedPaymentMethod = order.SimulatedPaymentMethod,
                };

                foreach (var line in targetLines)
                {
                    splitChild.Lines.Add(
                        new OrderLine
                        {
                            ProductId = line.ProductId,
                            Quantity = line.Quantity,
                            UnitPrice = line.UnitPrice,
                            LineCancellationState = OrderLineCancellationState.None,
                        });
                    _db.OrderLines.Remove(line);
                }

                order.TotalAmount -= amountDelta;
                if (order.TotalAmount < 0)
                {
                    order.TotalAmount = 0;
                }

                await _db.Orders.AddAsync(splitChild, cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);

            var parentId = order.Id;

            if (splitChild is not null)
            {
                var childFull = await _db.Orders
                    .Include(o => o.Lines)
                    .ThenInclude(l => l.Product)
                    .ThenInclude(p => p.Seller)
                    .AsNoTracking()
                    .FirstAsync(o => o.Id == splitChild.Id, cancellationToken);
                var childSellerIds = childFull.Lines
                    .Where(l => l.Product is not null)
                    .Select(l => l.Product!.SellerId)
                    .Distinct()
                    .ToList();
                await NotifySellersNewOrderAsync(
                    childFull.Id,
                    childFull.BuyerId,
                    childFull.TotalAmount,
                    childFull.Lines.Count,
                    childSellerIds,
                    cancellationToken);
            }

            var orderStillCancellationPending = order.Status == OrderStatus.CancellationPending;
            var anyPending = await _db.OrderLines.AnyAsync(
                l =>
                    l.OrderId == parentId
                    && (
                        l.LineCancellationState == OrderLineCancellationState.PendingSellerDecision
                        || (orderStillCancellationPending
                            && l.LineCancellationState == OrderLineCancellationState.None)),
                cancellationToken);

            if (!anyPending)
            {
                var remainingCount = await _db.OrderLines.CountAsync(l => l.OrderId == parentId, cancellationToken);
                if (remainingCount == 0)
                {
                    // เก็บแถว Orders ไว้ — ยกเลิกครบทุกร้านแล้ว แค่ตั้งสถานะ (ไม่ลบจาก DB)
                    order.Status = OrderStatus.Cancelled;
                    order.TotalAmount = 0;
                    order.PreCancellationStatus = null;
                    order.BuyerCancellationReason = null;
                    await _db.SaveChangesAsync(cancellationToken);

                    var cancelledStatus = OrderStatus.Cancelled.ToString();
                    foreach (var sid in sellerUserIdsForFinalNotify)
                    {
                        await _hub.Clients.Group(MarketplaceHub.SellerGroupName(sid))
                            .SendAsync(
                                "storeOrderStatusChanged",
                                new { orderId = order.Id, status = cancelledStatus },
                                cancellationToken);
                    }
                }
                else
                {
                    order.Status = order.PreCancellationStatus ?? OrderStatus.Paid;
                    order.PreCancellationStatus = null;
                    order.BuyerCancellationReason = null;
                    foreach (var rl in order.Lines)
                    {
                        rl.LineCancellationState = OrderLineCancellationState.None;
                    }

                    order.TotalAmount = order.Lines.Sum(l => l.UnitPrice * l.Quantity);
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);

            if (splitChild is not null)
            {
                var childDto = await _orders.GetByIdWithLinesAsync(splitChild.Id, cancellationToken);
                if (childDto is not null)
                {
                    await BroadcastOrderStatusAsync(childDto, cancellationToken);
                }
            }

            var full = await _orders.GetByIdWithLinesAsync(parentId, cancellationToken);
            if (full is not null)
            {
                await BroadcastOrderStatusAsync(full, cancellationToken);
                await BroadcastCancellationHubAsync(full, request.Approved, cancellationToken);

                // ไม่อนุมัติจนย้ายบรรทัดหมด — พ่อเป็นออเดอร์ว่างสถานะยกเลิก คืน DTO ออเดอร์ลูกให้ผู้เรียก (เดิมตอนลบพ่อจะคืนลูก)
                if (splitChild is not null
                    && full.Status == OrderStatus.Cancelled
                    && full.Lines.Count == 0)
                {
                    var childReturn = await _orders.GetByIdWithLinesAsync(splitChild.Id, cancellationToken);
                    if (childReturn is not null)
                    {
                        return Map(childReturn);
                    }
                }

                return Map(full);
            }

            return null;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void AppendSellerCancellationNote(Order order, int sellerUserId, string note)
    {
        var block = $"[ร้าน user #{sellerUserId}] {note}";
        order.CancellationReviewerNote = string.IsNullOrEmpty(order.CancellationReviewerNote)
            ? block
            : order.CancellationReviewerNote + "\n" + block;
    }

    private async Task NotifySellersNewOrderAsync(
        int orderId,
        int buyerId,
        decimal totalAmount,
        int lineCount,
        IEnumerable<int> sellerIds,
        CancellationToken cancellationToken)
    {
        foreach (var sellerId in sellerIds.Distinct())
        {
            await _hub.Clients.Group(MarketplaceHub.SellerGroupName(sellerId))
                .SendAsync(
                    "sellerNewOrder",
                    new
                    {
                        orderId,
                        buyerId,
                        totalAmount,
                        lineCount,
                    },
                    cancellationToken);
        }
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
            BuyerDisplayName = ResolveBuyerDisplayName(o),
            Status = o.Status,
            TotalAmount = o.TotalAmount,
            CreatedAtUtc = o.CreatedAtUtc,
            Lines = o.Lines.Select(l => MapLineDto(l, o.Status)).ToList(),
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
        dto.SplitSourceOrderId = o.SplitSourceOrderId;
    }

    /// <summary>
    /// ออเดอร์เก่าที่อยู่สถานะรอยกเลิกแต่บรรทัดยังเป็น None (ก่อนมีคอลัมน์/ก่อนตั้งค่า) ให้ถือว่ารอร้านพิจารณา
    /// </summary>
    private static bool LineRequiresSellerCancellationReview(OrderLine l, OrderStatus orderStatus) =>
        l.LineCancellationState == OrderLineCancellationState.PendingSellerDecision
        || (orderStatus == OrderStatus.CancellationPending
            && l.LineCancellationState == OrderLineCancellationState.None);

    private static int EffectiveLineCancellationStateForDto(OrderLine l, OrderStatus orderStatus) =>
        LineRequiresSellerCancellationReview(l, orderStatus)
            ? (int)OrderLineCancellationState.PendingSellerDecision
            : (int)l.LineCancellationState;

    private static OrderLineDto MapLineDto(OrderLine l, OrderStatus orderStatus)
    {
        return new OrderLineDto
        {
            ProductId = l.ProductId,
            ProductName = l.Product?.Name ?? string.Empty,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            SellerId = l.Product?.SellerId ?? 0,
            SellerDisplayName = l.Product?.Seller?.DisplayName ?? string.Empty,
            LineCancellationState = EffectiveLineCancellationStateForDto(l, orderStatus),
        };
    }

    /// <summary>มุมมองร้าน:เฉพาะบรรทัดที่ Product.SellerId ตรงกับร้าน — TotalAmount เป็นยอดรวมเฉพาะบรรทัดเหล่านั้น</summary>
    private static OrderDto MapForSeller(Order o, int sellerUserId)
    {
        var lines = o.Lines
            .Where(l => l.Product?.SellerId == sellerUserId)
            .Select(l => MapLineDto(l, o.Status))
            .ToList();
        var subtotal = lines.Sum(l => l.UnitPrice * l.Quantity);
        var dto = new OrderDto
        {
            Id = o.Id,
            BuyerId = o.BuyerId,
            BuyerDisplayName = ResolveBuyerDisplayName(o),
            Status = o.Status,
            TotalAmount = subtotal,
            CreatedAtUtc = o.CreatedAtUtc,
            Lines = lines,
        };
        CopyLifecycle(o, dto);
        return dto;
    }

    private static string ResolveBuyerDisplayName(Order o)
    {
        var name = o.Buyer?.DisplayName?.Trim();
        return string.IsNullOrEmpty(name) ? $"ผู้ซื้อ #{o.BuyerId}" : name;
    }
}
