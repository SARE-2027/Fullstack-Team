using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SARE.Domain.Users;

namespace SARE.Infrastructure.Persistence;

public sealed class DbInitializer(
    RoleManager<Role> roleManager,
    UserManager<User> userManager,
    IConfiguration configuration,
    ILogger<DbInitializer> logger)
{
    public async Task SeedAsync()
    {
        foreach (var roleName in UserRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new Role(roleName));
                if (roleResult.Succeeded)
                {
                    logger.LogInformation("Seeded role: {Role}", roleName);
                }
            }
        }

        var adminEmail = configuration["DefaultAdmin:Email"] ?? "admin@sare.com";
        var adminPassword = configuration["DefaultAdmin:Password"] ?? "Admin@123456";
        var adminName = configuration["DefaultAdmin:Name"] ?? "System Admin";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            var admin = new User
            {
                Id = Guid.NewGuid(),
                Name = adminName,
                Email = adminEmail.ToLowerInvariant(),
                UserName = adminEmail.ToLowerInvariant(),
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(admin, adminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, UserRoles.Admin);
                logger.LogInformation("Seeded default admin user: {Email}", adminEmail);
            }
            else
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                logger.LogWarning("Failed to seed default admin: {Errors}", errors);
            }
        }
    }
}
