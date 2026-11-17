using CommerceFlow.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CommerceFlow.Infrastructure.Persistence.Configurations.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(User.EmailMaxLength);
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(User.FullNameMaxLength);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(User.PasswordHashMaxLength);
        builder.Property(u => u.Role).IsRequired().HasMaxLength(User.RoleMaxLength);

        // One account per email: the register handler pre-checks (409), and
        // this index is the race-proof safety net behind that check.
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
