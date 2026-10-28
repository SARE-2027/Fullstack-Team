using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SARE.Domain.Cart;
using SARE.Domain.Catalog;

namespace SARE.Infrastructure.Persistence.Configurations.Cart;

public sealed class CartBasketAuditConfiguration : IEntityTypeConfiguration<CartBasketAudit>
{
    public void Configure(EntityTypeBuilder<CartBasketAudit> builder)
    {
        builder.Property(audit => audit.Id).ValueGeneratedNever();
        builder.HasOne<Session>().WithMany().HasForeignKey(audit => audit.SessionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(audit => audit.Items).WithOne().HasForeignKey(item => item.AuditId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(audit => audit.SessionId).IsUnique().HasFilter("completed_at_utc IS NULL");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_cart_basket_audits_completed", "(completed_at_utc IS NULL AND completed_by_user_id IS NULL) OR (completed_at_utc IS NOT NULL AND completed_at_utc >= started_at_utc AND completed_by_user_id IS NOT NULL)");
            table.HasCheckConstraint("ck_cart_basket_audits_weight", "last_stable_weight_g IS NULL OR last_stable_weight_g BETWEEN 0 AND 1000000");
        });
    }
}

public sealed class CartBasketAuditItemConfiguration : IEntityTypeConfiguration<CartBasketAuditItem>
{
    public void Configure(EntityTypeBuilder<CartBasketAuditItem> builder)
    {
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.WeightTolerancePercent).HasColumnType("numeric(5,2)");
        builder.Property(item => item.Barcode).HasMaxLength(64);
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(item => item.VariantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SessionItem>().WithMany().HasForeignKey(item => item.OriginalSessionItemId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_cart_basket_audit_items_price", "unit_price_minor >= 0");
            table.HasCheckConstraint("ck_cart_basket_audit_items_weight", "expected_weight_g > 0");
            table.HasCheckConstraint("ck_cart_basket_audit_items_weight_tolerance_percent", "weight_tolerance_percent BETWEEN 0 AND 100");
        });
    }
}
