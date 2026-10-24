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
            table.HasCheckConstraint("ck_session_items_unit_price_minor", "unit_price_minor >= 0");
            table.HasCheckConstraint("ck_session_items_removed_at", "removed_at IS NULL OR removed_at >= added_at");
            table.HasCheckConstraint("ck_session_items_presence", "basket_presence IN ('unconfirmed','inside','outside')");
            table.HasCheckConstraint("ck_session_items_weight", "expected_weight_g IS NULL OR expected_weight_g > 0");
            table.HasCheckConstraint("ck_session_items_weight_tolerance_percent", "weight_tolerance_percent BETWEEN 0 AND 100");
            table.HasCheckConstraint("ck_session_items_placement_sequence", "placement_sequence >= 0");
        });

        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.WeightTolerancePercent).HasColumnType("numeric(5,2)");
        builder.Property(item => item.BasketPresence).HasConversion(new SnakeCaseEnumConverter<BasketPresence>()).HasDefaultValue(BasketPresence.Unconfirmed);
        builder.Property(item => item.RemovedAt).IsConcurrencyToken();
        builder.Property(item => item.BasketTrackId).HasMaxLength(64);
        builder.Property(item => item.Barcode).IsRequired().HasMaxLength(64);
        builder.Property(item => item.BasketCameraId).HasMaxLength(64);
        builder.Property(item => item.AddedAt).HasDefaultValueSql("now()");
        builder.Property(item => item.Source)
            .HasConversion(new SnakeCaseEnumConverter<CartItemSource>());

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
