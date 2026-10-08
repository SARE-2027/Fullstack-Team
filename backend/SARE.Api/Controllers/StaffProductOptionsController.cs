using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Api.Authorization;
using SARE.Application.DTOs.Catalog;
using SARE.Application.Services;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/v1/staff/products/{productId:guid}/options")]
[Authorize(Policy = AuthorizationPolicies.CatalogRead)]
public sealed class StaffProductOptionsController(ProductOptionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductOptionResponse>>> GetOptions(Guid productId, CancellationToken ct) => Ok(await service.GetOptionsAsync(productId, ct));
    [HttpGet("{optionId:guid}")]
    public async Task<ActionResult<ProductOptionResponse>> GetOption(Guid productId, Guid optionId, CancellationToken ct) => Ok(await service.GetOptionAsync(productId, optionId, ct));
    [HttpGet("{optionId:guid}/values")]
    public async Task<ActionResult<IReadOnlyList<ProductOptionValueResponse>>> GetValues(Guid productId, Guid optionId, CancellationToken ct) => Ok(await service.GetValuesAsync(productId, optionId, ct));
    [HttpGet("{optionId:guid}/values/{valueId:guid}")]
    public async Task<ActionResult<ProductOptionValueResponse>> GetValue(Guid productId, Guid optionId, Guid valueId, CancellationToken ct) => Ok(await service.GetValueAsync(productId, optionId, valueId, ct));
}
