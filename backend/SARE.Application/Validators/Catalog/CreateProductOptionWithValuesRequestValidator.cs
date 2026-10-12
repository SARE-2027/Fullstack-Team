using FluentValidation;
using SARE.Application.DTOs.Catalog;

namespace SARE.Application.Validators.Catalog;

public sealed class CreateProductOptionWithValuesRequestValidator
    : AbstractValidator<CreateProductOptionWithValuesRequest>
{
    public CreateProductOptionWithValuesRequestValidator(IValidator<ProductOptionValueRequest> valueValidator)
    {
        RuleFor(request => request.NameAr).NotEmpty().MaximumLength(100);
        RuleFor(request => request.NameEn).NotEmpty().MaximumLength(100);
        RuleFor(request => request.Values).NotEmpty();
        RuleForEach(request => request.Values).NotNull().SetValidator(valueValidator);
    }
}
