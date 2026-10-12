using FluentValidation;
using SARE.Application.DTOs.Cart;

namespace SARE.Application.Validators.Cart;

public sealed class CartAddItemRequestValidator : AbstractValidator<CartAddItemRequest>
{
    public CartAddItemRequestValidator()
    {
        RuleFor(request => request.Barcode).NotEmpty().MaximumLength(64);
        RuleFor(request => request.MeasuredWeightG).GreaterThanOrEqualTo(0);
        RuleFor(request => request.Source).IsInEnum();
        RuleFor(request => request.EventId).NotEqual(Guid.Empty).When(request => request.EventId.HasValue);
    }
}
