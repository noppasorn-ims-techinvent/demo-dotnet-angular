using backend.Models.Dtos;
using backend.Models.Enums;

namespace backend.Services.Interfaces;

public interface IOrderService
{
    Task<OrderDto?> PlaceOrderAsync(int buyerId, PlaceOrderRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(int buyerId, CancellationToken cancellationToken = default);

    /// <summary>คำสั่งซื้อที่เกี่ยวกับร้านของผู้ขาย (บรรทัด + ยอดเฉพาะสินค้าของร้านนี้)</summary>
    Task<IReadOnlyList<OrderDto>> GetOrdersForMyStoreAsync(int sellerUserId, CancellationToken cancellationToken = default);

    Task<OrderDto?> GetOrderAsync(int userId, IReadOnlySet<string> roles, int orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetAllOrdersAsync(CancellationToken cancellationToken = default);

    Task<OrderDto?> UpdateStatusAsync(int orderId, OrderStatus status, CancellationToken cancellationToken = default);

    Task<bool> DeleteOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task<OrderDto?> SimulatePaymentAsync(int buyerId, int orderId, SimulatePaymentRequest request, CancellationToken cancellationToken = default);

    Task<OrderDto?> RequestCancellationAsync(int buyerId, int orderId, RequestCancellationRequest request, CancellationToken cancellationToken = default);

    /// <summary>แอดมิน หรือผู้ขายที่มีสินค้าในออเดอร์</summary>
    Task<OrderDto?> ReviewCancellationAsync(int reviewerUserId, IReadOnlySet<string> roles, int orderId, ReviewCancellationRequest request, CancellationToken cancellationToken = default);
}
