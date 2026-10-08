using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SARE.Domain.Users;

namespace SARE.Infrastructure.Persistence.Configurations.Users;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Property(token => token.Token)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(token => token.CreatedAtUtc)
            .HasDefaultValueSql("now()");

        builder.HasIndex(token => token.Token)
            .IsUnique();

        builder.HasIndex(token => token.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
