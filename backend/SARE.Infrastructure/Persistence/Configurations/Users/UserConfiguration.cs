using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SARE.Domain.Enums;
using SARE.Domain.Users;

namespace SARE.Infrastructure.Persistence.Configurations.Users;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable(table =>
            table.HasCheckConstraint("ck_users_role", "role IN ('customer', 'staff', 'admin')"));

        builder.Property(user => user.Name).HasMaxLength(100);
        builder.Property(user => user.Email).HasMaxLength(256);
        builder.Property(user => user.PasswordHash).HasMaxLength(256);
        builder.Property(user => user.NfcUid).HasMaxLength(64);
        builder.Property(user => user.CreatedAt).HasDefaultValueSql("now()");

        builder.Property(user => user.Role)
            .HasConversion(new SnakeCaseEnumConverter<UserRole>());

        builder.HasIndex(user => user.Email).IsUnique();
        builder.HasIndex(user => user.NfcUid).IsUnique();
    }
}
