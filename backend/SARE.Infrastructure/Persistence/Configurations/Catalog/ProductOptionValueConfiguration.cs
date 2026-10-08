using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SARE.Domain.Catalog;

namespace SARE.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductOptionValueConfiguration : IEntityTypeConfiguration<ProductOptionValue>
{
    public void Configure(EntityTypeBuilder<ProductOptionValue> builder)
    {
        builder.Property(value => value.ValueAr).HasMaxLength(100);
        builder.Property(value => value.ValueEn).HasMaxLength(100);

        builder.HasOne<ProductOption>()
            .WithMany()
            .HasForeignKey(value => value.ProductOptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(value => value.ProductOptionId);
    }
}
