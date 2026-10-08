using FluentValidation;
using SARE.Application.DTOs.Catalog;

namespace SARE.Application.Validators.Catalog;

public sealed class ProductRequestValidator : AbstractValidator<ProductRequest>
{
    public ProductRequestValidator()
    {
        RuleFor(request => request.CategoryId).NotEmpty();
        RuleFor(request => request.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(request => request.NameEn).NotEmpty().MaximumLength(150);
        RuleFor(request => request.ImageUrl).MaximumLength(500)
            .Must(IsImageLocation).WithMessage("ImageUrl must be an HTTP(S) URL or a path beginning with a single '/'.");
    }

    private static bool IsImageLocation(string? location)
    {
        if (location is null) return true;
        if (location.StartsWith('/') && !location.StartsWith("//", StringComparison.Ordinal)
            && !location.Contains('\\')) return true;
        return Uri.TryCreate(location, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https";
    }
}

public sealed class ProductStatusRequestValidator : AbstractValidator<ProductStatusRequest>
{
    public ProductStatusRequestValidator() => RuleFor(request => request.IsActive).NotNull();
}
