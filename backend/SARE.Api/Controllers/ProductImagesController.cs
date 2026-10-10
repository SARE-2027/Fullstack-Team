using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SARE.Api.Authorization;
using SARE.Application.Common.Exceptions;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;
using SARE.Application.Services;

namespace SARE.Api.Controllers;

[ApiController]
public sealed class ProductImagesController(ProductImageService service, IProductImageStore store) : ControllerBase
{
    [HttpPost("api/v1/admin/products/{productId:guid}/image")]
    [Authorize(Policy = AuthorizationPolicies.CatalogManage)]
    [RequestSizeLimit(6 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6 * 1024 * 1024)]
    public async Task<ActionResult<ProductResponse>> Upload(Guid productId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length > 5 * 1024 * 1024) throw new PayloadTooLargeException("Product images must not exceed 5 MiB.");
        await using var content = file.OpenReadStream();
        return Ok(await service.UploadAsync(productId, content, cancellationToken));
    }

    [HttpDelete("api/v1/admin/products/{productId:guid}/image")]
    [Authorize(Policy = AuthorizationPolicies.CatalogManage)]
    public async Task<IActionResult> Delete(Guid productId, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(productId, cancellationToken);
        return NoContent();
    }

    [HttpGet("api/v1/product-images/{fileName}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetImage(string fileName, CancellationToken cancellationToken)
    {
        var image = await store.OpenAsync(fileName, cancellationToken);
        if (image is null) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(image.Content, image.ContentType);
    }
}
