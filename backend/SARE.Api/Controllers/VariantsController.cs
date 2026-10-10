using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Application.Common.DTOs;
using SARE.Application.DTOs.Catalog;
using SARE.Application.Services;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/v1")]
[AllowAnonymous]
public sealed class VariantsController(ProductVariantService service) : ControllerBase
{
    [HttpGet("variants")]
    public async Task<ActionResult<PagedResponse<VariantLookupResponse>>> GetPage([FromQuery] VariantQuery query, CancellationToken ct) => Ok(await service.GetPublicPageAsync(query, ct));
    [HttpGet("variants/{variantId:guid}")]
    public async Task<ActionResult<VariantLookupResponse>> GetById(Guid variantId, CancellationToken ct) => Ok(await service.GetPublicByIdAsync(variantId, ct));
    [HttpGet("variants/by-barcode/{barcode}")]
    public async Task<ActionResult<VariantLookupResponse>> GetByBarcode([FromRoute] string barcode, CancellationToken ct) => Ok(await service.GetPublicByBarcodeAsync(barcode, ct));
    [HttpGet("variants/by-barcode")]
    public async Task<ActionResult<VariantLookupResponse>> Lookup([FromQuery] string barcode, CancellationToken ct) => Ok(await service.GetPublicByBarcodeAsync(barcode, ct));
    [HttpGet("products/{productId:guid}/variants")]
    public async Task<ActionResult<PagedResponse<VariantLookupResponse>>> GetForProduct(Guid productId, [FromQuery] VariantQuery query, CancellationToken ct) => Ok(await service.GetPublicForProductAsync(productId, query, ct));
}
