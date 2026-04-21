using System.Security.Claims;
using backend.Models.Dtos;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

public class ProductsController : BaseApiController
{
    private readonly IProductService _products;

    public ProductsController(IProductService products)
    {
        _products = products;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetCatalogAsync(CancellationToken cancellationToken)
    {
        return Ok(await _products.GetCatalogAsync(cancellationToken));
    }

    [HttpGet("{id:int}", Name = "GetProductById")]
    [Authorize]
    public async Task<ActionResult<ProductDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetMineAsync(CancellationToken cancellationToken)
    {
        var sellerId = GetUserId();
        if (sellerId is null)
        {
            return Unauthorized();
        }

        return Ok(await _products.GetSellerProductsAsync(sellerId.Value, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<ActionResult<ProductDto>> CreateAsync([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var sellerId = GetUserId();
        if (sellerId is null)
        {
            return Unauthorized();
        }

        var created = await _products.CreateAsync(sellerId.Value, request, cancellationToken);
        return CreatedAtRoute("GetProductById", new { id = created!.Id }, created);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<ActionResult<ProductDto>> UpdateAsync(int id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var sellerId = GetUserId();
        if (sellerId is null)
        {
            return Unauthorized();
        }

        var isAdmin = User.IsInRole("Admin");
        var updated = await _products.UpdateAsync(sellerId.Value, isAdmin, id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var sellerId = GetUserId();
        if (sellerId is null)
        {
            return Unauthorized();
        }

        var deleted = await _products.DeleteAsync(sellerId.Value, id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private int? GetUserId()
    {
        var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(v, out var id) ? id : null;
    }
}
