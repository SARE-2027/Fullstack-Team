using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Api.Authorization;
using SARE.Application.DTOs.Catalog;
using SARE.Application.Services;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize(Policy = AuthorizationPolicies.CatalogRead)]
public sealed class DashboardController(CatalogDashboardService service) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<CatalogDashboardResponse>> GetSummary(CancellationToken cancellationToken) =>
        Ok(await service.GetSummaryAsync(cancellationToken));
}
