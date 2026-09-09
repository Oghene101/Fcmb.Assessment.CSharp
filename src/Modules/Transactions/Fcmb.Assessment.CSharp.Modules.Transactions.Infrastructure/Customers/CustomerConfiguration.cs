using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Customers;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(c => c.FirstName)
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(c => c.LastName)
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(c => c.Email)
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.HasIndex(c => c.Email)
            .IsUnique();

        builder.Property(c => c.PhoneNumber)
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.HasIndex(c => c.PhoneNumber)
            .IsUnique();

        builder.Property(c => c.Dob)
            .IsRequired();

        builder.Property(c => c.CustomerType)
            .HasConversion(
                v => v.ToString().ToUpperInvariant(),
                v => Enum.Parse<CustomerType>(v, ignoreCase: true))
            .HasColumnType("varchar(50)")
            .IsUnicode(false) // Ensures LINQ parameters are also sent as VARCHAR (because of has conversion)
            .IsRequired();
    }
}
