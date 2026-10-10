namespace SARE.Application.DTOs.Catalog;

public sealed record ProductOptionValueResponse(Guid Id, string ValueAr, string ValueEn);
public sealed record ProductOptionResponse(
    Guid Id, string NameAr, string NameEn, IReadOnlyList<ProductOptionValueResponse> Values);
