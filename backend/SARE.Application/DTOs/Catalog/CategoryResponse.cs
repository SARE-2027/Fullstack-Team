namespace SARE.Application.DTOs.Catalog;

public sealed record CategoryResponse(Guid Id, string NameAr, string NameEn, int ProductCount);
