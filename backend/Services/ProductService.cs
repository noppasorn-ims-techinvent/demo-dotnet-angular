using backend.Hubs;
using backend.Models.Dtos;
using backend.Models.Entities;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace backend.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly IHubContext<MarketplaceHub> _hub;

    public ProductService(IProductRepository products, IHubContext<MarketplaceHub> hub)
    {
        _products = products;
        _hub = hub;
    }

    public async Task<IReadOnlyList<ProductDto>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var list = await _products.GetAllAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var p = await _products.GetByIdAsync(id, cancellationToken);
        return p is null ? null : Map(p);
    }

    public async Task<IReadOnlyList<ProductDto>> GetSellerProductsAsync(int sellerId, CancellationToken cancellationToken = default)
    {
        var list = await _products.GetBySellerIdAsync(sellerId, cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task<ProductDto?> CreateAsync(int sellerId, CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Product
        {
            SellerId = sellerId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            CreatedAtUtc = DateTime.UtcNow,
        };

        var created = await _products.AddAsync(entity, cancellationToken);
        return await GetByIdAsync(created.Id, cancellationToken);
    }

    public async Task<ProductDto?> UpdateAsync(int userId, bool isAdmin, int productId, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _products.GetByIdForUpdateAsync(productId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (!isAdmin && entity.SellerId != userId)
        {
            return null;
        }

        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.Price = request.Price;
        entity.StockQuantity = request.StockQuantity;
        await _products.UpdateAsync(entity, cancellationToken);

        if (isAdmin && entity.SellerId != userId)
        {
            await _hub.Clients.Group(MarketplaceHub.SellerGroupName(entity.SellerId))
                .SendAsync(
                    "productUpdatedByAdmin",
                    new { productId = entity.Id, name = entity.Name },
                    cancellationToken);
        }
        else if (!isAdmin)
        {
            await _hub.Clients.Group(MarketplaceHub.AdminsGroup)
                .SendAsync(
                    "productUpdatedBySeller",
                    new { productId = entity.Id, sellerId = entity.SellerId, name = entity.Name },
                    cancellationToken);
        }

        return await GetByIdAsync(productId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int userId, bool isAdmin, int productId, CancellationToken cancellationToken = default)
    {
        var entity = await _products.GetByIdForUpdateAsync(productId, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        if (!isAdmin && entity.SellerId != userId)
        {
            return false;
        }

        var ownerSellerId = entity.SellerId;
        var name = entity.Name;
        var id = entity.Id;

        await _products.DeleteAsync(entity, cancellationToken);

        await _hub.Clients.All.SendAsync("catalogProductRemoved", new { productId = id }, cancellationToken);

        if (isAdmin && ownerSellerId != userId)
        {
            await _hub.Clients.Group(MarketplaceHub.SellerGroupName(ownerSellerId))
                .SendAsync("productDeletedByAdmin", new { productId = id, name }, cancellationToken);
        }
        else if (!isAdmin)
        {
            await _hub.Clients.Group(MarketplaceHub.AdminsGroup)
                .SendAsync("productDeletedBySeller", new { productId = id, sellerId = ownerSellerId, name }, cancellationToken);
        }

        return true;
    }

    private static ProductDto Map(Product p)
    {
        return new ProductDto
        {
            Id = p.Id,
            SellerId = p.SellerId,
            SellerDisplayName = p.Seller?.DisplayName ?? string.Empty,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            StockQuantity = p.StockQuantity,
            CreatedAtUtc = p.CreatedAtUtc,
        };
    }
}
