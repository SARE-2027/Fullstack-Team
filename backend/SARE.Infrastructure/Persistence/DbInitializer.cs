using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SARE.Domain.Catalog;
using SARE.Domain.Enums;
using SARE.Domain.Users;

namespace SARE.Infrastructure.Persistence;

public sealed class DbInitializer(
    RoleManager<Role> roleManager,
    UserManager<User> userManager,
    AppDbContext dbContext,
    IConfiguration configuration,
    ILogger<DbInitializer> logger)
{
    public async Task SeedAsync()
    {
        // 1. Seed Roles
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

        // 2. Seed Default Admin
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

        // 3. Seed Demo Retail Catalog & Carts
        if (!await dbContext.Categories.AnyAsync())
        {
            var cannedCategory = new Category { Id = Guid.NewGuid(), NameAr = "معلبات", NameEn = "Canned Goods" };
            var beverageCategory = new Category { Id = Guid.NewGuid(), NameAr = "مشروبات", NameEn = "Beverages" };
            var snacksCategory = new Category { Id = Guid.NewGuid(), NameAr = "سناكس", NameEn = "Snacks" };

            dbContext.Categories.AddRange(cannedCategory, beverageCategory, snacksCategory);

            var tunaProduct = new Product { Id = Guid.NewGuid(), CategoryId = cannedCategory.Id, NameAr = "تونا صن شاين 180جم", NameEn = "Sunshine Tuna Chunk 180g", IsActive = true, UpdatedAt = DateTime.UtcNow };
            var tunaVariant = new ProductVariant { Id = Guid.NewGuid(), ProductId = tunaProduct.Id, Barcode = "6223000123456", PriceMinor = 6500, WeightG = 180, IsActive = true, UpdatedAt = DateTime.UtcNow };

            var juiceProduct = new Product { Id = Guid.NewGuid(), CategoryId = beverageCategory.Id, NameAr = "عصير جهينة بيور برتقال 1لتر", NameEn = "Juhayna Pure Orange Juice 1L", IsActive = true, UpdatedAt = DateTime.UtcNow };
            var juiceVariant = new ProductVariant { Id = Guid.NewGuid(), ProductId = juiceProduct.Id, Barcode = "6223000654321", PriceMinor = 4500, WeightG = 1050, IsActive = true, UpdatedAt = DateTime.UtcNow };

            var biscuitProduct = new Product { Id = Guid.NewGuid(), CategoryId = snacksCategory.Id, NameAr = "بسكويت بسكريم كاكاو 80جم", NameEn = "Biskrem Cocoa Filled Biscuits 80g", IsActive = true, UpdatedAt = DateTime.UtcNow };
            var biscuitVariant = new ProductVariant { Id = Guid.NewGuid(), ProductId = biscuitProduct.Id, Barcode = "6223000789012", PriceMinor = 1500, WeightG = 80, IsActive = true, UpdatedAt = DateTime.UtcNow };

            dbContext.Products.AddRange(tunaProduct, juiceProduct, biscuitProduct);
            dbContext.ProductVariants.AddRange(tunaVariant, juiceVariant, biscuitVariant);

            if (!await dbContext.Carts.AnyAsync())
            {
                dbContext.Carts.Add(new SARE.Domain.Cart.Cart
                {
                    Id = "CART_01",
                    Status = CartStatus.Active,
                    BatteryPct = 100,
                    LastSeenAt = DateTime.UtcNow
                });
            }

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Seeded demo catalog products and CART_01");
        }
    }
}
