using System.Text.Json.Serialization;

namespace SARE.Application.DTOs.Catalog;

public sealed record ProductResponse(
    Guid Id, Guid CategoryId, string NameAr, string NameEn, string? ImageUrl,
    bool IsActive, DateTime UpdatedAt, string CategoryNameAr, string CategoryNameEn,
    int VariantCount, int ActiveVariantCount)
{
    public decimal? WeightTolerancePercent { get; init; }
    public decimal EffectiveWeightTolerancePercent { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ProductOptionResponse>? Options { get; init; }
}
