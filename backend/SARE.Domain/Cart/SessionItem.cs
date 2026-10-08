using SARE.Domain.Common;
using SARE.Domain.Enums;

namespace SARE.Domain.Cart;

public class SessionItem : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid VariantId { get; set; }
    public int UnitPriceMinor { get; set; }
    public DetectionSource Source { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? RemovedAt { get; set; }

}
