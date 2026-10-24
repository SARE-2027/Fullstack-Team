using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SARE.Domain.Cart;
using SARE.Domain.Enums;

namespace SARE.Infrastructure.Persistence.Configurations.Cart;

public sealed class CartMovementItemConfiguration : IEntityTypeConfiguration<CartMovementItem>
{
    public void Configure(EntityTypeBuilder<CartMovementItem> builder)
    {
        builder.ToTable("cart_movement_items");
        builder.Property(x => x.Action).HasConversion(new SnakeCaseEnumConverter<CartItemAction>())
            .HasDefaultValue(CartItemAction.Enter).ValueGeneratedNever();
        builder.Property(x => x.Effect).HasConversion(new SnakeCaseEnumConverter<CartItemEffect>()).HasDefaultValue(CartItemEffect.None);
        builder.Property(x => x.TrackId).HasMaxLength(64);
        builder.Property(x => x.CameraId).HasMaxLength(64);
        builder.Property(x => x.ReasonCode).HasMaxLength(64);
        builder.HasIndex(x => new { x.MovementId, x.Position }).IsUnique();
        builder.HasOne<SARE.Domain.Catalog.ProductVariant>().WithMany().HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_cart_movement_items_source", "source IN ('vision', 'scanner', 'manual')");
            table.HasCheckConstraint("ck_cart_movement_items_outcome", "outcome IN ('accepted', 'corrected', 'rejected', 'unknown')");
            table.HasCheckConstraint("ck_cart_movement_items_confidence", "confidence IS NULL OR confidence BETWEEN 0 AND 1");
            table.HasCheckConstraint("ck_cart_movement_items_action", "action IN ('scan','enter','exit','cancel_scan')");
            table.HasCheckConstraint("ck_cart_movement_items_effect", "effect IN ('none','added','removed','placement_confirmed','no_change','pending_added','pending_removed')");
            table.HasCheckConstraint("ck_cart_movement_items_snapshot", "(expected_weight_g IS NULL OR expected_weight_g > 0) AND (unit_price_minor IS NULL OR unit_price_minor >= 0)");
            table.HasCheckConstraint("ck_cart_movement_items_weight_tolerance_percent", "weight_tolerance_percent BETWEEN 0 AND 100");
            table.HasCheckConstraint("ck_cart_movement_items_position", "position >= 0");
        });

        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.WeightTolerancePercent).HasColumnType("numeric(5,2)");
        builder.Property(item => item.DetectedBarcode).HasMaxLength(64);
        builder.Property(item => item.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(item => item.Source)
            .HasConversion(new SnakeCaseEnumConverter<CartItemSource>());
        builder.Property(item => item.Outcome)
            .HasConversion(new SnakeCaseEnumConverter<CartItemOutcome>());

        builder.HasIndex(item => item.SessionItemId);

        builder.HasOne<SessionItem>()
            .WithMany()
            .HasForeignKey(item => item.SessionItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
