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

}
