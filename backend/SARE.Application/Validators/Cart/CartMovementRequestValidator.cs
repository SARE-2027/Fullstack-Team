using FluentValidation;
using SARE.Application.DTOs.Cart;
using SARE.Domain.Enums;

namespace SARE.Application.Validators.Cart;

public sealed class CartMovementRequestValidator : AbstractValidator<CartMovementRequest>
{
    public CartMovementRequestValidator()
    {
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.MovementId).NotEmpty();
        RuleFor(x => x.DeviceBootId).NotEmpty();
        RuleFor(x => x.Sequence).InclusiveBetween(1, int.MaxValue - 1);
        RuleFor(x => x.IsComplete).Equal(true).WithMessage("Send one finished movement after collecting its evidence.");
        RuleFor(x => x.StartedAtUtc).Must(x => x != default && x.Kind == DateTimeKind.Utc);
        RuleFor(x => x.EndedAtUtc).Must(x => x.Kind == DateTimeKind.Utc)
            .GreaterThanOrEqualTo(x => x.StartedAtUtc).Must(x => x <= DateTime.UtcNow.AddMinutes(1));
        RuleFor(x => x.ModelVersion).MaximumLength(50);
        RuleFor(x => x.Items).NotNull().Must(x => x is null || x.Count <= 100);
        RuleFor(x => x).Must(x => x.Items is null || x.Items.Count > 0 || x.Weight is not null)
            .WithMessage("A report needs item evidence or a basket weight snapshot.");
        RuleForEach(x => x.Items).NotNull().ChildRules(item =>
        {
            item.RuleFor(x => x.Action).IsInEnum();
            item.RuleFor(x => x.Source).Must(x => x is CartItemSource.Scanner or CartItemSource.Vision);
            item.RuleFor(x => x.Source).Equal(CartItemSource.Scanner).When(x => x.Action is CartItemAction.Scan or CartItemAction.CancelScan);
            item.RuleFor(x => x.SessionItemId).NotNull().When(x => x.Action == CartItemAction.CancelScan);
            item.RuleFor(x => x.Barcode).MaximumLength(64);
            item.RuleFor(x => x.Barcode).NotEmpty().When(x => x.Action is CartItemAction.Scan or CartItemAction.CancelScan);
            item.RuleFor(x => x.Confidence).InclusiveBetween(0f, 1f);
            item.RuleFor(x => x.TrackId).MaximumLength(64);
            item.RuleFor(x => x.CameraId).MaximumLength(64);
            item.RuleFor(x => x.SessionItemId).NotEqual(Guid.Empty).When(x => x.SessionItemId.HasValue);
            // الإضافة والدخول لا يحتاجان ID من الجهاز؛ المرجع يخص الإخراج أو إلغاء السكان فقط.
            item.RuleFor(x => x.SessionItemId).Null().When(x => x.Action is CartItemAction.Scan or CartItemAction.Enter);
        });
        When(x => x.Weight is not null, () =>
        {
            RuleFor(x => x.Weight!.BeforeG).InclusiveBetween(0, 1_000_000);
            RuleFor(x => x.Weight!.AfterG).InclusiveBetween(0, 1_000_000);
            RuleFor(x => x.Weight!.ToleranceG).InclusiveBetween(0, 100);
            RuleFor(x => x.Weight!.IsStable).Equal(true).WithMessage("Wait for stable basket weight before sending a movement.");
        });
        RuleFor(x => x.Weight).NotNull().When(x => x.Items is not null
            && x.Items.Any(item => item?.Action is CartItemAction.Enter or CartItemAction.Exit));
        RuleFor(x => x.Items).Must(items => items is null || items.Where(x => x is not null && !string.IsNullOrWhiteSpace(x.TrackId))
            .GroupBy(x => (x.Action, x.CameraId, x.TrackId)).All(group => group.Count() == 1))
            .WithMessage("Report a tracked crossing once per direction, not once per frame.");
    }
}
