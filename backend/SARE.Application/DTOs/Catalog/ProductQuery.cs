namespace SARE.Application.DTOs.Catalog;

public record ProductQuery
{
    public string? Search { get; init; }
    public Guid? CategoryId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; } = "nameEn";
    public bool SortDescending { get; init; }
}

public sealed record AdminProductQuery : ProductQuery
{
    public bool? IsActive { get; init; }
}
