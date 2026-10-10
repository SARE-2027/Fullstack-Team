using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Api.Authorization;
using SARE.Application.DTOs.Catalog;
using SARE.Application.Services;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/v1/admin/products/{productId:guid}/options")]
[Authorize(Policy = AuthorizationPolicies.CatalogManage)]
public sealed class AdminProductOptionsController(ProductOptionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductOptionResponse>>> GetOptions(Guid productId, CancellationToken ct) => Ok(await service.GetOptionsAsync(productId, ct));
    [HttpGet("{optionId:guid}")]
    public async Task<ActionResult<ProductOptionResponse>> GetOption(Guid productId, Guid optionId, CancellationToken ct) => Ok(await service.GetOptionAsync(productId, optionId, ct));
    [HttpPost]
    public async Task<ActionResult<ProductOptionResponse>> CreateOption(Guid productId, ProductOptionRequest request, CancellationToken ct)
    {
        var option = await service.CreateOptionAsync(productId, request, ct);
        return CreatedAtAction(nameof(GetOption), new { productId, optionId = option.Id }, option);
    }
    [HttpPut("{optionId:guid}")]
    public async Task<ActionResult<ProductOptionResponse>> UpdateOption(Guid productId, Guid optionId, ProductOptionRequest request, CancellationToken ct) => Ok(await service.UpdateOptionAsync(productId, optionId, request, ct));
    [HttpDelete("{optionId:guid}")]
    public async Task<IActionResult> DeleteOption(Guid productId, Guid optionId, CancellationToken ct) { await service.DeleteOptionAsync(productId, optionId, ct); return NoContent(); }
    [HttpGet("{optionId:guid}/values")]
    public async Task<ActionResult<IReadOnlyList<ProductOptionValueResponse>>> GetValues(Guid productId, Guid optionId, CancellationToken ct) => Ok(await service.GetValuesAsync(productId, optionId, ct));
    [HttpGet("{optionId:guid}/values/{valueId:guid}")]
    public async Task<ActionResult<ProductOptionValueResponse>> GetValue(Guid productId, Guid optionId, Guid valueId, CancellationToken ct) => Ok(await service.GetValueAsync(productId, optionId, valueId, ct));
    [HttpPost("{optionId:guid}/values")]
    public async Task<ActionResult<ProductOptionValueResponse>> CreateValue(Guid productId, Guid optionId, ProductOptionValueRequest request, CancellationToken ct)
    {
        var value = await service.CreateValueAsync(productId, optionId, request, ct);
        return CreatedAtAction(nameof(GetValue), new { productId, optionId, valueId = value.Id }, value);
    }
    [HttpPut("{optionId:guid}/values/{valueId:guid}")]
    public async Task<ActionResult<ProductOptionValueResponse>> UpdateValue(Guid productId, Guid optionId, Guid valueId, ProductOptionValueRequest request, CancellationToken ct) => Ok(await service.UpdateValueAsync(productId, optionId, valueId, request, ct));
    [HttpDelete("{optionId:guid}/values/{valueId:guid}")]
    public async Task<IActionResult> DeleteValue(Guid productId, Guid optionId, Guid valueId, CancellationToken ct) { await service.DeleteValueAsync(productId, optionId, valueId, ct); return NoContent(); }
}
