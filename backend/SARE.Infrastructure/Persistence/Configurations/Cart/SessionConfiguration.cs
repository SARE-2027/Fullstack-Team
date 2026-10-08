using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SARE.Domain.Cart;
using SARE.Domain.Enums;
using SARE.Domain.Users;
using CartEntity = SARE.Domain.Cart.Cart;

namespace SARE.Infrastructure.Persistence.Configurations.Cart;

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("ck_sessions_status", "status IN ('open', 'closed', 'abandoned')");
            table.HasCheckConstraint("ck_sessions_close_reason", "close_reason IS NULL OR close_reason IN ('staff_closed', 'abandoned')");
            table.HasCheckConstraint("ck_sessions_total_minor", "total_minor >= 0");
            table.HasCheckConstraint("ck_sessions_closed_at", "closed_at IS NULL OR closed_at >= started_at");
        });

        builder.Property(session => session.CartId).HasMaxLength(32);
        builder.Property(session => session.StartedAt).HasDefaultValueSql("now()");
        builder.Property(session => session.LastActivityAt).HasDefaultValueSql("now()");

        builder.Property(session => session.Status)
            .HasConversion(new SnakeCaseEnumConverter<SessionStatus>());
        builder.Property(session => session.CloseReason)
            .HasConversion(new SnakeCaseEnumConverter<CloseReason>());

        builder.HasOne<CartEntity>()
            .WithMany()
            .HasForeignKey(session => session.CartId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(session => session.ClosedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(session => session.CartId);
        builder.HasIndex(session => session.UserId);
        builder.HasIndex(session => session.Status);
    }
}
