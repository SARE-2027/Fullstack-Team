using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SARE.Domain.Catalog;

namespace SARE.Infrastructure.Persistence.Configurations.Catalog;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(product => product.NameAr).HasMaxLength(150);
        builder.Property(product => product.NameEn).HasMaxLength(150);
        builder.Property(product => product.ImageUrl).HasMaxLength(500);
        builder.Property(product => product.UpdatedAt).HasDefaultValueSql("now()").IsConcurrencyToken();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(product => product.CategoryId);
    }
}
