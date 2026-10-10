using FluentValidation;
using SARE.Application.DTOs.Catalog;

namespace SARE.Application.Validators.Catalog;

public sealed class CategoryRequestValidator : AbstractValidator<CategoryRequest>
{
    public CategoryRequestValidator()
    {
        RuleFor(request => request.NameAr).NotEmpty().MaximumLength(100);
        RuleFor(request => request.NameEn).NotEmpty().MaximumLength(100);
    }
}
