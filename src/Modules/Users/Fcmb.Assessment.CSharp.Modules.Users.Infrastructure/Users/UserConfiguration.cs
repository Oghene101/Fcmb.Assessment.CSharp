using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.FirstName)
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(u => u.Dob)
            .IsRequired();

        builder.Property(u => u.IdentityId)
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.HasIndex(u => u.IdentityId)
            .IsUnique();

        builder.Property(u => u.UserType)
            .HasConversion(
                v => v.ToString().ToUpperInvariant(),
                v => Enum.Parse<UserType>(v, ignoreCase: true))
            .HasColumnType("varchar(50)")
            .IsUnicode(false) // Ensures LINQ parameters are also sent as VARCHAR (because of has conversion)
            .IsRequired();
    }
}
