using SARE.Domain.Common;

namespace SARE.Domain.Catalog;

public class ProductOptionValue : BaseEntity
{
    public Guid ProductOptionId { get; set; }
    public string ValueAr { get; set; } = string.Empty;
    public string ValueEn { get; set; } = string.Empty;

}
