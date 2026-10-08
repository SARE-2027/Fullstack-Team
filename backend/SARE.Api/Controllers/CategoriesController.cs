using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Application.Common.DTOs;
using SARE.Application.DTOs.Catalog;
using SARE.Application.Services;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
[AllowAnonymous]
public sealed class CategoriesController(CategoryService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CategorySummaryResponse>>> GetPage(
        [FromQuery] CategoryQuery query, CancellationToken cancellationToken) =>
        Ok(await service.GetPublicPageAsync(query, cancellationToken));

    [HttpGet("{categoryId:guid}")]
    public async Task<ActionResult<CategorySummaryResponse>> GetById(Guid categoryId, CancellationToken cancellationToken) =>
        Ok(await service.GetPublicByIdAsync(categoryId, cancellationToken));
}
