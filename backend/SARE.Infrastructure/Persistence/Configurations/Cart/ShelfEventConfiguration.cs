using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SARE.Domain.Cart;
using SARE.Domain.Catalog;

namespace SARE.Infrastructure.Persistence.Configurations.Cart;

public sealed class ShelfEventConfiguration : IEntityTypeConfiguration<ShelfEvent>
{
    public void Configure(EntityTypeBuilder<ShelfEvent> builder)
    {
        builder.ToTable(table =>
            table.HasCheckConstraint("ck_shelf_events_matched_session", "NOT is_matched OR matched_session_id IS NOT NULL"));

        builder.Property(shelfEvent => shelfEvent.ShelfId).HasMaxLength(32);
        builder.Property(shelfEvent => shelfEvent.CreatedAt).HasDefaultValueSql("now()");

        builder.HasOne<ProductVariant>()
            .WithMany()
            .HasForeignKey(shelfEvent => shelfEvent.VariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Session>()
            .WithMany()
            .HasForeignKey(shelfEvent => shelfEvent.MatchedSessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(shelfEvent => shelfEvent.VariantId);
        builder.HasIndex(shelfEvent => shelfEvent.MatchedSessionId);
        builder.HasIndex(shelfEvent => shelfEvent.CreatedAt);
    }
}
