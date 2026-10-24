using SARE.Domain.Common;
using SARE.Domain.Enums;

namespace SARE.Domain.Cart;

public class Session : BaseEntity
{
    public string CartId { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Open;
    public int TotalMinor { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? ClosedBy { get; set; }
    public CloseReason? CloseReason { get; set; }
    public int LastMovementSequence { get; set; }
    public int? BasketWeightG { get; set; }
    public int PendingWeightG { get; set; }
    public int PendingWeightUncertaintyG { get; set; }
    public DateTime? StaffRequestedAt { get; set; }
    public string? StaffRequestReason { get; set; }
    // حد زمني صادر من السيرفر؛ التقارير المؤجلة من فترة الجرد لا تُحاسب بعد انتهائه.
    public DateTime? LastAuditCompletedAtUtc { get; set; }
    public BasketState BasketState { get; set; } = BasketState.Verified;

}
