using Microsoft.AspNetCore.Mvc;
using SARE.Application.Common.Interfaces;
using SARE.Application.DTOs.Catalog;

namespace SARE.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService productService) : ControllerBase
{
    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<List<ProductResponseDto>>> GetProducts(CancellationToken cancellationToken)
    {
        var products = await productService.GetAllProductsAsync(cancellationToken);
        return Ok(products);
    }

    // GET: api/products/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponseDto>> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        var product = await productService.GetProductByIdAsync(id, cancellationToken);
        if (product == null) return NotFound(new { message = "Product not found" });

        return Ok(product);
    }

    // GET: api/products/by-barcode/{barcode}
    [HttpGet("by-barcode/{barcode}")]
    public async Task<ActionResult<ProductResponseDto>> GetByBarcode(string barcode, CancellationToken cancellationToken)
    {
        var product = await productService.GetProductByBarcodeAsync(barcode, cancellationToken);
        if (product == null) return NotFound(new { message = $"No product found with barcode '{barcode}'" });

        return Ok(product);
    }

    // POST: api/products
    [HttpPost]
    public async Task<ActionResult<ProductResponseDto>> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var created = await productService.CreateProductAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetProduct), new { id = created.Id }, created);
    }
}
