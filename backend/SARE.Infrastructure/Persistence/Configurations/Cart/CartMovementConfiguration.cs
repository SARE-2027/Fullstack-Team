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
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.MovementId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Session>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.SessionId, x.Sequence }).IsUnique();
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_cart_movements_status", "status IN ('accepted','needs_confirmation','cancelled')");
            table.HasCheckConstraint("ck_cart_movements_weights", "(weight_before_g IS NULL AND weight_after_g IS NULL) OR (weight_before_g IS NOT NULL AND weight_after_g IS NOT NULL AND weight_before_g >= 0 AND weight_after_g >= 0)");
            table.HasCheckConstraint("ck_cart_movements_tolerance", "weight_tolerance_g BETWEEN 0 AND 100");
            table.HasCheckConstraint("ck_cart_movements_sequence", "sequence > 0");
            table.HasCheckConstraint("ck_cart_movements_time", "ended_at_utc >= started_at_utc");
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
