using FluentValidation;
using SARE.Application.DTOs.Catalog;

namespace SARE.Application.Validators.Catalog;

public sealed class CategoryQueryValidator : AbstractValidator<CategoryQuery>
{
    public CategoryQueryValidator()
    {
        RuleFor(query => query.Search).MaximumLength(100);
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query)
            .Must(query => (long)(query.Page - 1) * query.PageSize <= int.MaxValue)
            .When(query => query.Page >= 1 && query.PageSize is >= 1 and <= 100)
            .OverridePropertyName(nameof(CategoryQuery.Page))
            .WithMessage("The requested page is too large.");
    }
}
