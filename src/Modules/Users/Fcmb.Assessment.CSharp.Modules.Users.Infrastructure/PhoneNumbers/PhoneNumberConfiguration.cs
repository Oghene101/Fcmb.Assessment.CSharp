using Fcmb.Assessment.CSharp.Modules.Users.Domain.PhoneNumbers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.PhoneNumbers;

internal sealed class PhoneNumberConfiguration : IEntityTypeConfiguration<PhoneNumber>
{
    public void Configure(EntityTypeBuilder<PhoneNumber> builder)
    {
        builder.Property(c => c.Number)
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.HasIndex(c => c.Number)
            .IsUnique();

        builder
            .HasIndex(e => e.UserId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
