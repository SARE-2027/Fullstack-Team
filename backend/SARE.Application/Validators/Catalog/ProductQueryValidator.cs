using FluentValidation;
using SARE.Application.DTOs.Catalog;

namespace SARE.Application.Validators.Catalog;

public class ProductQueryValidator : AbstractValidator<ProductQuery>
{
    private static readonly string[] SortFields = ["nameAr", "nameEn", "updatedAt"];

    public ProductQueryValidator()
    {
        RuleFor(query => query.Search).MaximumLength(150);
        RuleFor(query => query.CategoryId).NotEqual(Guid.Empty).When(query => query.CategoryId.HasValue);
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.SortBy).Must(sort => SortFields.Contains(sort, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortBy must be nameAr, nameEn, or updatedAt.");
        RuleFor(query => query)
            .Must(query => (long)(query.Page - 1) * query.PageSize <= int.MaxValue)
            .When(query => query.Page >= 1 && query.PageSize is >= 1 and <= 100)
            .OverridePropertyName(nameof(ProductQuery.Page))
            .WithMessage("The requested page is too large.");
    }
}

public sealed class AdminProductQueryValidator : AbstractValidator<AdminProductQuery>
{
    public AdminProductQueryValidator() => Include(new ProductQueryValidator());
}
