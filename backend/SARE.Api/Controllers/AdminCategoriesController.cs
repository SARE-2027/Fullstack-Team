using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Api.Authorization;
using SARE.Application.Common.DTOs;
using SARE.Application.DTOs.Catalog;
using SARE.Application.Services;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/v1/admin/categories")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public sealed class AdminCategoriesController(CategoryService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CategoryResponse>>> GetPage(
        [FromQuery] CategoryQuery query, CancellationToken cancellationToken) =>
        Ok(await service.GetPageAsync(query, cancellationToken));

    [HttpGet("{categoryId:guid}")]
    public async Task<ActionResult<CategoryResponse>> GetById(Guid categoryId, CancellationToken cancellationToken) =>
        Ok(await service.GetByIdAsync(categoryId, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(
        [FromBody] CategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { categoryId = category.Id }, category);
    }

    [HttpPut("{categoryId:guid}")]
    public async Task<ActionResult<CategoryResponse>> Update(
        Guid categoryId, [FromBody] CategoryRequest request, CancellationToken cancellationToken) =>
        Ok(await service.UpdateAsync(categoryId, request, cancellationToken));

    [HttpDelete("{categoryId:guid}")]
    public async Task<IActionResult> Delete(Guid categoryId, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(categoryId, cancellationToken);
        return NoContent();
    }
}
