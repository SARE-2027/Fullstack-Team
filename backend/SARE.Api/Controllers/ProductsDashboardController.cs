using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Api.Authorization;
using SARE.Application.DTOs.Catalog;
using SARE.Application.Services;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard/products")]
[Authorize(Policy = AuthorizationPolicies.CatalogRead)]
public sealed class ProductsDashboardController(ProductService service) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ProductDashboardResponse>> GetSummary(CancellationToken cancellationToken) =>
        Ok(await service.GetDashboardAsync(cancellationToken));
}
