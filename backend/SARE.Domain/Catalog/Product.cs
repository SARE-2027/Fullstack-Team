using SARE.Domain.Common;

namespace SARE.Domain.Catalog;

public class Product : BaseEntity
{
    public Guid CategoryId { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; }

}
