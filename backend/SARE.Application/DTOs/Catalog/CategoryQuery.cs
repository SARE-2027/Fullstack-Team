namespace SARE.Application.DTOs.Catalog;

public sealed record CategoryQuery
{
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
