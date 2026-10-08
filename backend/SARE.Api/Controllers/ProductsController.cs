using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Application.Common.DTOs;
using SARE.Application.DTOs.Catalog;
using SARE.Application.Services;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
[AllowAnonymous]
public sealed class ProductsController(ProductService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<ProductSummaryResponse>>> GetPage(
        [FromQuery] ProductQuery query, CancellationToken cancellationToken) =>
        Ok(await service.GetPublicPageAsync(query, cancellationToken));

    [HttpGet("{productId:guid}")]
    public async Task<ActionResult<ProductPublicDetailResponse>> GetById(Guid productId, CancellationToken cancellationToken) =>
        Ok(await service.GetPublicByIdAsync(productId, cancellationToken));
}
