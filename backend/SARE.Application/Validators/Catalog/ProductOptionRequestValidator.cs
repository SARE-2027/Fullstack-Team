using FluentValidation;
using SARE.Application.DTOs.Catalog;

namespace SARE.Application.Validators.Catalog;

public sealed class ProductOptionRequestValidator : AbstractValidator<ProductOptionRequest>
{
    public ProductOptionRequestValidator()
    {
        RuleFor(request => request.NameAr).NotEmpty().MaximumLength(100);
        RuleFor(request => request.NameEn).NotEmpty().MaximumLength(100);
    }
}

public sealed class ProductOptionValueRequestValidator : AbstractValidator<ProductOptionValueRequest>
{
    public ProductOptionValueRequestValidator()
    {
        RuleFor(request => request.ValueAr).NotEmpty().MaximumLength(100);
        RuleFor(request => request.ValueEn).NotEmpty().MaximumLength(100);
    }
}
