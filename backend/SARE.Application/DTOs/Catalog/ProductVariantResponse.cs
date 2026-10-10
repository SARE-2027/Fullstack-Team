namespace SARE.Application.DTOs.Catalog;

public sealed record ProductVariantResponse(
    Guid Id, Guid ProductId, string Barcode, int PriceMinor, int WeightG,
    bool IsActive, DateTime UpdatedAt, IReadOnlyList<Guid> OptionValueIds);

public sealed record ProductVariantSummaryResponse(
    Guid Id, string Barcode, int PriceMinor, int WeightG, IReadOnlyList<Guid> OptionValueIds);
