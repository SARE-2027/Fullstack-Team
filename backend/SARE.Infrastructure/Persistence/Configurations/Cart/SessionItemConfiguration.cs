using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SARE.Domain.Cart;
using SARE.Domain.Catalog;
using SARE.Domain.Enums;

namespace SARE.Infrastructure.Persistence.Configurations.Cart;

public sealed class SessionItemConfiguration : IEntityTypeConfiguration<SessionItem>
{
    public void Configure(EntityTypeBuilder<SessionItem> builder)
    {
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_session_items_source", "source IN ('vision', 'scanner', 'manual')");
            table.HasCheckConstraint("ck_session_items_zone", "zone IN ('basket', 'tray')");
            table.HasCheckConstraint("ck_session_items_unit_price_minor", "unit_price_minor >= 0");
            table.HasCheckConstraint("ck_session_items_removed_at", "removed_at IS NULL OR removed_at >= added_at");
        });

        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.AddedAt).HasDefaultValueSql("now()");
        builder.Property(item => item.Source)
            .HasConversion(new SnakeCaseEnumConverter<DetectionSource>());
        builder.Property(item => item.Zone)
            .HasConversion(new SnakeCaseEnumConverter<CartZone>());

        builder.HasOne<Session>()
            .WithMany()
            .HasForeignKey(item => item.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(item => item.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.SessionId);
        builder.HasIndex(item => item.VariantId);
    }
}
