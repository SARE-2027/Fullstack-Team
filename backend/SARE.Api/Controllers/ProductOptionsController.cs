using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Application.DTOs.Catalog;
using SARE.Application.Services;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/v1/products/{productId:guid}/options")]
[AllowAnonymous]
public sealed class ProductOptionsController(ProductOptionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductOptionResponse>>> GetOptions(Guid productId, CancellationToken ct) => Ok(await service.GetPublicOptionsAsync(productId, ct));
    [HttpGet("{optionId:guid}")]
    public async Task<ActionResult<ProductOptionResponse>> GetOption(Guid productId, Guid optionId, CancellationToken ct) => Ok(await service.GetPublicOptionAsync(productId, optionId, ct));
    [HttpGet("{optionId:guid}/values")]
    public async Task<ActionResult<IReadOnlyList<ProductOptionValueResponse>>> GetValues(Guid productId, Guid optionId, CancellationToken ct) => Ok(await service.GetPublicValuesAsync(productId, optionId, ct));
    [HttpGet("{optionId:guid}/values/{valueId:guid}")]
    public async Task<ActionResult<ProductOptionValueResponse>> GetValue(Guid productId, Guid optionId, Guid valueId, CancellationToken ct) => Ok(await service.GetPublicValueAsync(productId, optionId, valueId, ct));
}
