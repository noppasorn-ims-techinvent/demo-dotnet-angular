using backend.Models.Dtos;

namespace backend.Services.Interfaces;

public interface IProductService
{
    Task<IReadOnlyList<ProductDto>> GetCatalogAsync(CancellationToken cancellationToken = default);

    Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductDto>> GetSellerProductsAsync(int sellerId, CancellationToken cancellationToken = default);

    Task<ProductDto?> CreateAsync(int sellerId, CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductDto?> UpdateAsync(int userId, bool isAdmin, int productId, UpdateProductRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int sellerId, int productId, CancellationToken cancellationToken = default);
}
