using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SARE.Domain.Catalog;
using SARE.Domain.Cart;
using SARE.Domain.Users;
using CartEntity = SARE.Domain.Cart.Cart;

namespace SARE.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, TimeProvider? timeProvider = null)
    : IdentityDbContext<User, Role, Guid>(options)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductOption> ProductOptions => Set<ProductOption>();
    public DbSet<ProductOptionValue> ProductOptionValues => Set<ProductOptionValue>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<VariantOptionValue> VariantOptionValues => Set<VariantOptionValue>();
    public DbSet<CartEntity> Carts => Set<CartEntity>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionItem> SessionItems => Set<SessionItem>();
    public DbSet<DetectionEvent> DetectionEvents => Set<DetectionEvent>();
    public DbSet<ShelfEvent> ShelfEvents => Set<ShelfEvent>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public override int SaveChanges() => SaveChanges(acceptAllChangesOnSuccess: true);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampTimestamps()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            switch (entry.Entity)
            {
                case Product product:
                    product.UpdatedAt = now;
                    break;
                case ProductVariant variant:
                    variant.UpdatedAt = now;
                    break;
                case User user when entry.State == EntityState.Added && user.CreatedAt == default:
                    user.CreatedAt = now;
                    break;
                case RefreshToken refreshToken when entry.State == EntityState.Added && refreshToken.CreatedAtUtc == default:
                    refreshToken.CreatedAtUtc = now;
                    break;
                case Session session:
                    if (entry.State == EntityState.Added && session.StartedAt == default)
                    {
                        session.StartedAt = now;
                    }
                    session.LastActivityAt = now;
                    break;
                case SessionItem item when entry.State == EntityState.Added && item.AddedAt == default:
                    item.AddedAt = now;
                    break;
                case DetectionEvent detection when entry.State == EntityState.Added && detection.CreatedAt == default:
                    detection.CreatedAt = now;
                    break;
                case ShelfEvent shelf when entry.State == EntityState.Added && shelf.CreatedAt == default:
                    shelf.CreatedAt = now;
                    break;
            }
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSnakeCaseNamingConvention();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("user_role_claims");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        modelBuilder.Entity<RefreshToken>().ToTable("refresh_tokens");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
