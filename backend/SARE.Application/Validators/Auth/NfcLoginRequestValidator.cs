using FluentValidation;
using SARE.Application.DTOs.Auth;

namespace SARE.Application.Validators.Auth;

public sealed class NfcLoginRequestValidator : AbstractValidator<NfcLoginRequest>
{
    public NfcLoginRequestValidator()
    {
        RuleFor(x => x.NfcUid)
            .NotEmpty().WithMessage("NFC UID is required.")
            .MaximumLength(64).WithMessage("NFC UID must not exceed 64 characters.");
    }
}
