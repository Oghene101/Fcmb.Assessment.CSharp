using Fcmb.Assessment.CSharp.Modules.Users.Domain.Emails;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Emails;

internal sealed class EmailConfiguration : IEntityTypeConfiguration<Email>
{
    public void Configure(EntityTypeBuilder<Email> builder)
    {
        builder.Property(c => c.Address)
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.HasIndex(c => c.Address)
            .IsUnique();

        builder
            .HasIndex(e => e.UserId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
