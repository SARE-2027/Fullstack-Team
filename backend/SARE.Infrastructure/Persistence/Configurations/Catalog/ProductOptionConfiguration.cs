using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SARE.Domain.Catalog;

namespace SARE.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductOptionConfiguration : IEntityTypeConfiguration<ProductOption>
{
    public void Configure(EntityTypeBuilder<ProductOption> builder)
    {
        builder.Property(option => option.NameAr).HasMaxLength(100);
        builder.Property(option => option.NameEn).HasMaxLength(100);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(option => option.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(option => option.ProductId);
    }
}
