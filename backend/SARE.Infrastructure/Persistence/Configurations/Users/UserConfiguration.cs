using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SARE.Domain.Users;

namespace SARE.Infrastructure.Persistence.Configurations.Users;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(user => user.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.NfcUid)
            .HasMaxLength(64);

        builder.Property(user => user.CreatedAt)
            .HasDefaultValueSql("now()");

        builder.HasIndex(user => user.NfcUid)
            .IsUnique();
    }
}
