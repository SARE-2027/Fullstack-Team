using Microsoft.AspNetCore.Identity;

namespace SARE.Domain.Users;

public class User : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? NfcUid { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
