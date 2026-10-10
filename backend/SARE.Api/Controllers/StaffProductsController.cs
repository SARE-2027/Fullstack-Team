using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Api.Authorization;
using SARE.Application.Common.DTOs;
using SARE.Application.DTOs.Catalog;
using SARE.Application.Services;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/v1/staff/products")]
[Authorize(Policy = AuthorizationPolicies.CatalogRead)]
public sealed class StaffProductsController(ProductService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> GetPage(
        [FromQuery] AdminProductQuery query, CancellationToken cancellationToken) =>
        Ok(await service.GetPageAsync(query, cancellationToken));

    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<ProductDetailResponse>> GetById(Guid productId, CancellationToken cancellationToken) =>
        Ok(await service.GetByIdAsync(productId, cancellationToken));
}
