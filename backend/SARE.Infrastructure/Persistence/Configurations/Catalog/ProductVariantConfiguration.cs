using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SARE.Domain.Catalog;

namespace SARE.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.Property(variant => variant.Barcode).HasMaxLength(64);
        builder.Property(variant => variant.UpdatedAt).HasDefaultValueSql("now()");

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_product_variants_barcode", "length(trim(barcode)) > 0");
            table.HasCheckConstraint("ck_product_variants_price_minor", "price_minor >= 0");
            table.HasCheckConstraint("ck_product_variants_weight_g", "weight_g > 0");
        });

        builder.HasIndex(variant => variant.Barcode).IsUnique();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(variant => variant.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(variant => variant.ProductId);
    }
}
