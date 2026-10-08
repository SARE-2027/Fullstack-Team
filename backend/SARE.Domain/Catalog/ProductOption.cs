using SARE.Domain.Common;

namespace SARE.Domain.Catalog;

public class ProductOption : BaseEntity
{
    public Guid ProductId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;

}
