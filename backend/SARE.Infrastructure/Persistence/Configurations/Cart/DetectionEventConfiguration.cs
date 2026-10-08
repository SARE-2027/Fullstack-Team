using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SARE.Domain.Cart;
using SARE.Domain.Enums;

namespace SARE.Infrastructure.Persistence.Configurations.Cart;

public sealed class DetectionEventConfiguration : IEntityTypeConfiguration<DetectionEvent>
{
    public void Configure(EntityTypeBuilder<DetectionEvent> builder)
    {
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_detection_events_source", "source IN ('vision', 'scanner', 'manual')");
            table.HasCheckConstraint("ck_detection_events_zone", "zone IN ('basket', 'tray')");
            table.HasCheckConstraint("ck_detection_events_outcome", "outcome IN ('accepted', 'corrected', 'rejected', 'unknown')");
            table.HasCheckConstraint("ck_detection_events_confidence", "confidence IS NULL OR confidence BETWEEN 0 AND 1");
        });

        builder.Property(detection => detection.Id).ValueGeneratedNever();
        builder.Property(detection => detection.DetectedBarcode).HasMaxLength(64);
        builder.Property(detection => detection.FinalBarcode).HasMaxLength(64);
        builder.Property(detection => detection.ModelVersion).HasMaxLength(50);
        builder.Property(detection => detection.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(detection => detection.Source)
            .HasConversion(new SnakeCaseEnumConverter<DetectionSource>());
        builder.Property(detection => detection.Zone)
            .HasConversion(new SnakeCaseEnumConverter<CartZone>());
        builder.Property(detection => detection.Outcome)
            .HasConversion(new SnakeCaseEnumConverter<DetectionOutcome>());

        builder.HasIndex(detection => new { detection.SessionId, detection.CreatedAt });
        builder.HasIndex(detection => detection.SessionItemId);

        builder.HasOne<Session>()
            .WithMany()
            .HasForeignKey(detection => detection.SessionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SessionItem>()
            .WithMany()
            .HasForeignKey(detection => detection.SessionItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
