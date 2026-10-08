using FluentValidation;
using SARE.Application.DTOs.Catalog;

namespace SARE.Application.Validators.Catalog;

public sealed class VariantRequestValidator : AbstractValidator<VariantRequest>
{
    public VariantRequestValidator()
    {
        RuleFor(request => request.Barcode).NotEmpty().MaximumLength(64);
        RuleFor(request => request.PriceMinor).GreaterThanOrEqualTo(0);
        RuleFor(request => request.WeightG).GreaterThan(0);
        RuleFor(request => request.OptionValueIds).NotNull();
        RuleFor(request => request.OptionValueIds).Must(ids => ids!.Distinct().Count() == ids.Count)
            .When(request => request.OptionValueIds is not null).WithMessage("OptionValueIds must not contain duplicate IDs.");
        RuleForEach(request => request.OptionValueIds).NotEmpty();
    }
}
public sealed class VariantStatusRequestValidator : AbstractValidator<VariantStatusRequest>
{
    public VariantStatusRequestValidator() => RuleFor(request => request.IsActive).NotNull();
}
public class VariantQueryValidator : AbstractValidator<VariantQuery>
{
    public VariantQueryValidator()
    {
        RuleFor(query => query.Search).MaximumLength(150);
        RuleFor(query => query.ProductId).NotEqual(Guid.Empty).When(query => query.ProductId.HasValue);
        RuleFor(query => query.CategoryId).NotEqual(Guid.Empty).When(query => query.CategoryId.HasValue);
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query).Must(query => (long)(query.Page - 1) * query.PageSize <= int.MaxValue)
            .When(query => query.Page >= 1 && query.PageSize is >= 1 and <= 100)
            .OverridePropertyName(nameof(VariantQuery.Page)).WithMessage("The requested page is too large.");
    }
}
public sealed class AdminVariantQueryValidator : AbstractValidator<AdminVariantQuery>
{
    public AdminVariantQueryValidator() => Include(new VariantQueryValidator());
}
