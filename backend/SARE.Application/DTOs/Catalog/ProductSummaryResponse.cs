namespace SARE.Application.DTOs.Catalog;

public sealed record ProductSummaryResponse(
    Guid Id, Guid CategoryId, string NameAr, string NameEn, string? ImageUrl,
    string CategoryNameAr, string CategoryNameEn, int? MinPriceMinor, int? MaxPriceMinor);
