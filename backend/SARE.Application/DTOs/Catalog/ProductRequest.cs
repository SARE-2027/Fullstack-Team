namespace SARE.Application.DTOs.Catalog;

public sealed record ProductRequest(
    Guid CategoryId, string? NameAr, string? NameEn, string? ImageUrl, bool IsActive = true);

public sealed record ProductStatusRequest(bool? IsActive);
