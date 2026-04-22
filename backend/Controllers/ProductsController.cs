using System.Security.Claims;
using backend.DTO;
using backend.Models.Dtos;
using backend.Services.Interfaces;
using backend.Utilities;
using backend.Utilities.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route(Constant.AuthorizeConfig.RouteController)]
public class ProductsController : ControllerBase
{
    private readonly IProductService _products;
    private readonly AppSettings _appSettings;
    private readonly ITrace _trace;

    public ProductsController(IProductService products, AppSettings appSettings, ITrace trace)
    {
        _products = products;
        _appSettings = appSettings;
        _trace = trace;
    }

    [HttpGet]
    [Authorize]
    public async Task<Result<IReadOnlyList<ProductDto>>> GetCatalogAsync(CancellationToken cancellationToken)
    {
        var list = await _products.GetCatalogAsync(cancellationToken);
        Result<IReadOnlyList<ProductDto>> result = new(_trace)
        {
            Success = true,
            Message = _appSettings.SuccessMessage.Success,
            Data = list,
        };

        return result;
    }

    [HttpGet("{id:int}", Name = "GetProductById")]
    [Authorize]
    public async Task<Result<ProductDto>> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        Result<ProductDto> result = new(_trace);
        var product = await _products.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.NotFound;
            return result;
        }

        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = product;
        return result;
    }

    [HttpGet]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<Result<IReadOnlyList<ProductDto>>> GetMineAsync(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ProductDto>> result = new(_trace);
        var sellerId = GetUserId();
        if (sellerId is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.Unauthorized;
            return result;
        }

        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = await _products.GetSellerProductsAsync(sellerId.Value, cancellationToken);
        return result;
    }

    [HttpPost]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<Result<ProductDto>> CreateAsync([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        Result<ProductDto> result = new(_trace);
        var sellerId = GetUserId();
        if (sellerId is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.Unauthorized;
            return result;
        }

        var created = await _products.CreateAsync(sellerId.Value, request, cancellationToken);
        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = created!;
        return result;
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<Result<ProductDto>> UpdateAsync(int id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        Result<ProductDto> result = new(_trace);
        var sellerId = GetUserId();
        if (sellerId is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.Unauthorized;
            return result;
        }

        var isAdmin = User.IsInRole("Admin");
        var updated = await _products.UpdateAsync(sellerId.Value, isAdmin, id, request, cancellationToken);
        if (updated is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.NotFound;
            return result;
        }

        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = updated;
        return result;
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Seller,Admin")]
    public async Task<Result<object>> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        Result<object> result = new(_trace);
        var sellerId = GetUserId();
        if (sellerId is null)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.Unauthorized;
            return result;
        }

        var isAdmin = User.IsInRole("Admin");
        var deleted = await _products.DeleteAsync(sellerId.Value, isAdmin, id, cancellationToken);
        if (!deleted)
        {
            result.Success = false;
            result.Message = _appSettings.ErrorMessage.NotFound;
            return result;
        }

        result.Success = true;
        result.Message = _appSettings.SuccessMessage.Success;
        result.Data = null;
        return result;
    }

    private int? GetUserId()
    {
        var v = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(v, out var id) ? id : null;
    }
}
