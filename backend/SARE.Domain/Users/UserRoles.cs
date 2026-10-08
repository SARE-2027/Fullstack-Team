namespace SARE.Domain.Users;

public static class UserRoles
{
    public const string Admin = "Admin";
    public const string Staff = "Staff";
    public const string Customer = "Customer";

    public static readonly IReadOnlyList<string> All = [Admin, Staff, Customer];
}
