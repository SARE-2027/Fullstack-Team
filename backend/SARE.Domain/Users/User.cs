using SARE.Domain.Common;
using SARE.Domain.Enums;

namespace SARE.Domain.Users;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.Customer;
    public string? NfcUid { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

}
