using SARE.Domain.Common;

namespace SARE.Domain.Catalog;

public class ProductVariant : BaseEntity
{
    public Guid ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public int PriceMinor { get; set; }
    public int WeightG { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; }

}
