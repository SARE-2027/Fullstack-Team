using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SARE.Domain.Catalog;

namespace SARE.Infrastructure.Persistence.Configurations.Catalog;

public sealed class VariantOptionValueConfiguration : IEntityTypeConfiguration<VariantOptionValue>
{
    public void Configure(EntityTypeBuilder<VariantOptionValue> builder)
    {
        builder.HasKey(link => new { link.VariantId, link.OptionValueId });

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(link => link.VariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ProductOptionValue>()
            .WithMany()
            .HasForeignKey(link => link.OptionValueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(link => link.OptionValueId);
    }
}
