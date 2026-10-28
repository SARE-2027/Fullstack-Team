namespace SARE.Application.DTOs.Cart;

public enum CartAuditAction
{
    Start,
    RemoveItem,
    Commit
}

// نفس غلاف الأوامر الحالي يحافظ على الترتيب وإعادة الإرسال، حتى لقرارات الأدمن.
public record CartAuditCommand(Guid EventId, Guid MovementId, int Sequence, Guid DeviceBootId,
    string ExpectedVersion)
{
    public Guid? AuditId { get; init; }
    public Guid? AuditItemId { get; init; }
    public string? Reason { get; init; }
}

public record CartAuditItemResponse(Guid Id, Guid VariantId, string Barcode, string NameAr, string NameEn,
    int UnitPriceMinor, int ExpectedWeightG, decimal WeightTolerancePercent);

public record CartAuditResponse(Guid Id, Guid StartedByUserId, DateTime StartedAtUtc,
    int? LastStableWeightG, IReadOnlyList<CartAuditItemResponse> Items)
{
    public long TotalMinor => Items.Sum(item => (long)item.UnitPriceMinor);
}
