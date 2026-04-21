using backend.Models.Dtos;
using backend.Models.Enums;

namespace backend.Services.Interfaces;

public interface IOrderService
{
    Task<OrderDto?> PlaceOrderAsync(int buyerId, PlaceOrderRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetMyOrdersAsync(int buyerId, CancellationToken cancellationToken = default);

    Task<OrderDto?> GetOrderAsync(int userId, IReadOnlySet<string> roles, int orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetAllOrdersAsync(CancellationToken cancellationToken = default);

    Task<OrderDto?> UpdateStatusAsync(int orderId, OrderStatus status, CancellationToken cancellationToken = default);

    Task<bool> DeleteOrderAsync(int orderId, CancellationToken cancellationToken = default);
}
