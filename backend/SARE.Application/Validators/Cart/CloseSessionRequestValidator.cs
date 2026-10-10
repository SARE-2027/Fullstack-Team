using FluentValidation;
using SARE.Application.DTOs.Cart;

namespace SARE.Application.Validators.Cart;

public class CloseSessionRequestValidator : AbstractValidator<CloseSessionRequest>
{
    public CloseSessionRequestValidator()
    {
        RuleFor(x => x.ClosedByUserId)
            .Must(id => id != Guid.Empty).WithMessage("معرف المستخدم لا يمكن أن يكون فارغاً عند تمريره")
            .When(x => x.ClosedByUserId.HasValue);

        RuleFor(x => x.Reason)
            .IsInEnum().WithMessage("سبب الإغلاق غير صالح")
            .When(x => x.Reason.HasValue);
    }
}
