namespace SARE.Application.DTOs.Catalog;

public sealed record VariantRequest(string? Barcode, int PriceMinor, int WeightG, bool IsActive, IReadOnlyList<Guid>? OptionValueIds);
public sealed record VariantStatusRequest(bool? IsActive);

public record VariantQuery
{
    public string? Search { get; init; }
    public Guid? ProductId { get; init; }
    public Guid? CategoryId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
public sealed record AdminVariantQuery : VariantQuery
{
    public bool? IsActive { get; init; }
}
public sealed record VariantLookupResponse(Guid Id, Guid ProductId, Guid CategoryId,
    string ProductNameAr, string ProductNameEn, string Barcode, int PriceMinor, int WeightG,
    IReadOnlyList<Guid> OptionValueIds);
