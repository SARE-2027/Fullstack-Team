using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Api.Authorization;
using SARE.Application.Common.DTOs;
using SARE.Application.DTOs.Catalog;
using SARE.Application.Services;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = AuthorizationPolicies.CatalogManage)]
public sealed class AdminVariantsController(ProductVariantService service) : ControllerBase
{
    [HttpGet("variants")]
    public async Task<ActionResult<PagedResponse<ProductVariantResponse>>> GetPage([FromQuery] AdminVariantQuery query, CancellationToken ct) => Ok(await service.GetPageAsync(query, ct));
    [HttpGet("variants/{variantId:guid}")]
    public async Task<ActionResult<ProductVariantResponse>> GetById(Guid variantId, CancellationToken ct) => Ok(await service.GetByIdAsync(variantId, ct));
    [HttpGet("variants/by-barcode/{barcode}")]
    public async Task<ActionResult<ProductVariantResponse>> GetByBarcode([FromRoute] string barcode, CancellationToken ct) => Ok(await service.GetByBarcodeAsync(barcode, ct));
    [HttpGet("variants/by-barcode")]
    public async Task<ActionResult<ProductVariantResponse>> Lookup([FromQuery] string barcode, CancellationToken ct) => Ok(await service.GetByBarcodeAsync(barcode, ct));
    [HttpGet("products/{productId:guid}/variants")]
    public async Task<ActionResult<PagedResponse<ProductVariantResponse>>> GetForProduct(Guid productId, [FromQuery] AdminVariantQuery query, CancellationToken ct) => Ok(await service.GetForProductAsync(productId, query, ct));
    [HttpPost("products/{productId:guid}/variants")]
    public async Task<ActionResult<ProductVariantResponse>> Create(Guid productId, VariantRequest request, CancellationToken ct)
    {
        var variant = await service.CreateAsync(productId, request, ct);
        return CreatedAtAction(nameof(GetById), new { variantId = variant.Id }, variant);
    }
    [HttpPut("products/{productId:guid}/variants/{variantId:guid}")]
    public async Task<ActionResult<ProductVariantResponse>> Update(Guid productId, Guid variantId, VariantRequest request, CancellationToken ct) => Ok(await service.UpdateAsync(productId, variantId, request, ct));
    [HttpPatch("products/{productId:guid}/variants/{variantId:guid}/status")]
    public async Task<ActionResult<ProductVariantResponse>> SetStatus(Guid productId, Guid variantId, VariantStatusRequest request, CancellationToken ct) => Ok(await service.SetStatusAsync(productId, variantId, request, ct));
}
