using Microsoft.AspNetCore.Identity;

namespace SARE.Domain.Users;

public class Role : IdentityRole<Guid>
{
    public Role()
    {
    }

    public Role(string roleName) : base(roleName)
    {
    }
}
