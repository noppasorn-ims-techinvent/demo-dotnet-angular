using backend.Models.Entities;

namespace backend.Repositories.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdWithLinesAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetByBuyerIdAsync(int buyerId, CancellationToken cancellationToken = default);

    Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(Order order, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default);
}
