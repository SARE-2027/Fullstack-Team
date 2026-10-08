using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Api.Authorization;
using SARE.Application.Common.DTOs;
using SARE.Application.DTOs.Catalog;
using SARE.Application.Services;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/v1/admin/products")]
[Authorize(Policy = AuthorizationPolicies.CatalogManage)]
public sealed class AdminProductsController(ProductService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> GetPage(
        [FromQuery] AdminProductQuery query, CancellationToken cancellationToken) =>
        Ok(await service.GetPageAsync(query, cancellationToken));

    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<ProductDetailResponse>> GetById(Guid productId, CancellationToken cancellationToken) =>
        Ok(await service.GetByIdAsync(productId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create([FromBody] ProductRequest request, CancellationToken cancellationToken)
    {
        var product = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { productId = product.Id }, product);
    }

    [HttpPut("{productId:guid}")]
    public async Task<ActionResult<ProductResponse>> Update(
        Guid productId, [FromBody] ProductRequest request, CancellationToken cancellationToken) =>
        Ok(await service.UpdateAsync(productId, request, cancellationToken));

    [HttpPatch("{productId:guid}/status")]
    public async Task<ActionResult<ProductResponse>> SetStatus(
        Guid productId, [FromBody] ProductStatusRequest request, CancellationToken cancellationToken) =>
        Ok(await service.SetStatusAsync(productId, request, cancellationToken));
}
