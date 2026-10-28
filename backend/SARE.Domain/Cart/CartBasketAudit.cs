namespace SARE.Domain.Cart;

// جرد واحد يحتفظ بالقائمة التي راجعها الأدمن حتى بعد اعتمادها.
public class CartBasketAudit
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid StartedByUserId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public int? LastStableWeightG { get; set; }
    public int? LastWeightSequence { get; set; }
    public DateTime? LastWeightAtUtc { get; set; }
    public int LastScanSequence { get; set; }
    public Guid? CompletedByUserId { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public ICollection<CartBasketAuditItem> Items { get; set; } = new List<CartBasketAuditItem>();
}

public class CartBasketAuditItem
{
    public Guid Id { get; set; }
    public Guid AuditId { get; set; }
    public Guid VariantId { get; set; }
    public Guid? OriginalSessionItemId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public int UnitPriceMinor { get; set; }
    public int ExpectedWeightG { get; set; }
    public decimal WeightTolerancePercent { get; set; }
    public DateTime ScannedAtUtc { get; set; }
    public bool IsRemoved { get; set; }
}
