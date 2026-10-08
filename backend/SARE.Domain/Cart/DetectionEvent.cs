using SARE.Domain.Common;
using SARE.Domain.Enums;

namespace SARE.Domain.Cart;

public class DetectionEvent : BaseEntity
{
    public Guid SessionId { get; set; }
    public Guid? SessionItemId { get; set; }
    public DetectionSource Source { get; set; }
    public string? DetectedBarcode { get; set; }
    public float? Confidence { get; set; }
    public int WeightDeltaG { get; set; }
    public DetectionOutcome Outcome { get; set; } = DetectionOutcome.Unknown;
    public string? FinalBarcode { get; set; }
    public string? ModelVersion { get; set; }
    public DateTime CreatedAt { get; set; }
}
