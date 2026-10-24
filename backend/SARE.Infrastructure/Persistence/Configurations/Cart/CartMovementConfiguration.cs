using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SARE.Domain.Cart;
using SARE.Domain.Enums;

namespace SARE.Infrastructure.Persistence.Configurations.Cart;

public sealed class CartMovementConfiguration : IEntityTypeConfiguration<CartMovement>
{
    public void Configure(EntityTypeBuilder<CartMovement> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Status).HasConversion(new SnakeCaseEnumConverter<CartMovementStatus>());
        builder.Property(x => x.ReasonCode).HasMaxLength(64);
        builder.Property(x => x.ModelVersion).HasMaxLength(50);
        builder.Property(x => x.PendingWeightBeforeG).HasDefaultValue(0);
        builder.Property(x => x.PendingWeightAfterG).HasDefaultValue(0);
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.MovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Session>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.SessionId, x.Sequence }).IsUnique();
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_cart_movements_status", "status IN ('accepted','needs_confirmation','cancelled')");
            table.HasCheckConstraint("ck_cart_movements_weights", "(weight_before_g IS NULL AND weight_after_g IS NULL) OR (weight_before_g IS NOT NULL AND weight_after_g IS NOT NULL AND weight_before_g >= 0 AND weight_after_g >= 0)");
            table.HasCheckConstraint("ck_cart_movements_sequence", "sequence > 0");
            table.HasCheckConstraint("ck_cart_movements_time", "ended_at_utc >= started_at_utc");
            table.HasCheckConstraint("ck_cart_movements_pending_weight", "pending_weight_before_g >= 0 AND pending_weight_after_g >= 0 AND (unconfirmed_exit_weight_g IS NULL OR unconfirmed_exit_weight_g >= 0)");
            table.HasCheckConstraint("ck_cart_movements_exit_uncertainty", "exit_weight_uncertainty_g >= 0");
            table.HasCheckConstraint("ck_cart_movements_exit_pending", "exit_pending_weight_g >= 0 AND exit_pending_uncertainty_g BETWEEN 0 AND 1000000");
        });
    }
}

public sealed class CartEventReceiptConfiguration : IEntityTypeConfiguration<CartEventReceipt>
{
    public void Configure(EntityTypeBuilder<CartEventReceipt> builder)
    {
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PayloadHash).HasMaxLength(64);
        builder.HasOne<Session>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CartMovement>().WithMany().HasForeignKey(x => x.MovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.SessionId, x.Sequence }).IsUnique();
        builder.ToTable(table => table.HasCheckConstraint("ck_cart_event_receipts_sequence", "sequence > 0"));
    }
}
