using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SARE.Domain.Enums;
using CartEntity = SARE.Domain.Cart.Cart;

namespace SARE.Infrastructure.Persistence.Configurations.Cart;

public sealed class CartConfiguration : IEntityTypeConfiguration<CartEntity>
{
    public void Configure(EntityTypeBuilder<CartEntity> builder)
    {
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_carts_status", "status IN ('active', 'disabled')");
            table.HasCheckConstraint("ck_carts_battery_pct", "battery_pct IS NULL OR battery_pct BETWEEN 0 AND 100");
        });

        builder.Property(cart => cart.Id).HasMaxLength(32);
        builder.Property(cart => cart.TokenHash).HasMaxLength(256);
        builder.Property(cart => cart.SwVersion).HasMaxLength(32);

        builder.Property(cart => cart.Status)
            .HasConversion(new SnakeCaseEnumConverter<CartStatus>());

        builder.Property<uint>("Version").IsRowVersion();
    }
}
