using SARE.Domain.Common;
using SARE.Domain.Enums;

namespace SARE.Domain.Cart;

public class CartMovement : BaseEntity
{
    public Guid SessionId { get; set; }
    public int Sequence { get; set; }
    public Guid DeviceBootId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime EndedAtUtc { get; set; }
    public int? WeightBeforeG { get; set; }
    public int? WeightAfterG { get; set; }
    public bool HasBasketChange { get; set; }
    public string? ModelVersion { get; set; }
    public CartMovementStatus Status { get; set; } = CartMovementStatus.NeedsConfirmation;
    public string? ReasonCode { get; set; }
    public int PendingWeightBeforeG { get; set; }
    public int PendingWeightAfterG { get; set; }
    public int? UnconfirmedExitWeightG { get; set; }
    public int ExitWeightUncertaintyG { get; set; }
    // رصيد متاح لهذا الخروج وقت حدوثه، يتناقص فقط عند حسم خروج أقدم في الطابور.
    public int ExitPendingWeightG { get; set; }
    public int ExitPendingUncertaintyG { get; set; }
    // الحسم قرار جديد يشير للتقرير الأصلي؛ لا يعاد تشغيل التقرير على الفاتورة.
    public Guid? RelatedMovementId { get; set; }
    public Guid? ResolvedByMovementId { get; set; }
    public bool ConfirmedByCustomer { get; set; }
    public Guid? AuditId { get; set; }
    public ICollection<CartMovementItem> Items { get; set; } = new List<CartMovementItem>();
}
