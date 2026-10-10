using FluentValidation;
using SARE.Application.DTOs.Cart;

namespace SARE.Application.Validators.Cart;

public class UpdateCartTelemetryRequestValidator : AbstractValidator<UpdateCartTelemetryRequest>
{
    public UpdateCartTelemetryRequestValidator()
    {
        RuleFor(x => x.BatteryPct)
            .InclusiveBetween((short)0, (short)100)
            .WithMessage("نسبة البطارية يجب أن تكون بين 0 و 100")
            .When(x => x.BatteryPct.HasValue);

        RuleFor(x => x.SwVersion)
            .MaximumLength(32)
            .WithMessage("إصدار السوفتوير طويل جداً")
            .When(x => x.SwVersion is not null);

        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("حالة العربة غير صالحة")
            .When(x => x.Status.HasValue);
    }
}
