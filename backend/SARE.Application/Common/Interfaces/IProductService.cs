using SARE.Application.DTOs.Catalog;

namespace SARE.Application.Common.Interfaces;

public interface IProductService
{
    Task<List<ProductResponseDto>> GetAllProductsAsync(CancellationToken cancellationToken = default);
    Task<ProductResponseDto?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductResponseDto?> GetProductByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
    Task<ProductResponseDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
}
