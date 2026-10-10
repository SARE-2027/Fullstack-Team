using FluentValidation;
using SARE.Application.DTOs.Cart;

namespace SARE.Application.Validators.Cart;

public class StartSessionRequestValidator : AbstractValidator<StartSessionRequest>
{
    public StartSessionRequestValidator()
    {
        RuleFor(x => x.UserId).NotEqual(Guid.Empty).When(x => x.UserId.HasValue);
        RuleFor(x => x).Must(x => !x.UserId.HasValue || string.IsNullOrWhiteSpace(x.NfcUid))
            .WithMessage("Choose UserId or NfcUid, not both.").OverridePropertyName("UserId");
        RuleFor(x => x.CartId)
            .NotEmpty().WithMessage("معرف السلة (CartId) مطلوب")
            .MaximumLength(32).WithMessage("معرف السلة يجب ألا يتجاوز 32 حرفاً");

        RuleFor(x => x.NfcUid)
            .MaximumLength(64).WithMessage("معرف الـ NFC يجب ألا يتجاوز 64 حرفاً")
            .When(x => !string.IsNullOrEmpty(x.NfcUid));
    }
}
