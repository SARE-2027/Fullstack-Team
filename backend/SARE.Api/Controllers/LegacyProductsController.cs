using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Api.Authorization;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;

namespace SARE.Api.Controllers;

// Preserve main's existing kiosk API while the versioned catalog supports the full admin workflow.
[ApiController]
[Route("api/products")]
public sealed class LegacyProductsController(IProductService service) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<ProductResponseDto>>> GetProducts(CancellationToken ct) =>
        Ok(await service.GetAllProductsAsync(ct));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductResponseDto>> GetProduct(Guid id, CancellationToken ct)
    {
        var product = await service.GetProductByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("by-barcode/{barcode}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductResponseDto>> GetByBarcode(string barcode, CancellationToken ct)
    {
        var product = await service.GetProductByBarcodeAsync(barcode, ct);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.CatalogManage)]
    public async Task<ActionResult<ProductResponseDto>> CreateProduct(CreateProductRequest request, CancellationToken ct)
    {
        var product = await service.CreateProductAsync(request, ct);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }
}
