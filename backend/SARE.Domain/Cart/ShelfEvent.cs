using SARE.Domain.Common;

namespace SARE.Domain.Cart;

public class ShelfEvent : BaseEntity
{
    public string ShelfId { get; set; } = string.Empty;
    public Guid VariantId { get; set; }
    public int WeightDeltaG { get; set; }
    public bool IsMatched { get; set; }
    public Guid? MatchedSessionId { get; set; }
    public DateTime CreatedAt { get; set; }
}
