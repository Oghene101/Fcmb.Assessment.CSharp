using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Accounts;

internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.Property(a => a.AccountName)
            .HasColumnType("varchar(150)")
            .IsRequired();
        
        builder.HasIndex(a => a.AccountName)
            .IsUnique();
        
        builder.Property(a => a.AccountNumber)
            .HasColumnType("char(10)")
            .IsRequired();

        builder.HasIndex(a => a.AccountNumber)
            .IsUnique();
        
        builder.Property(a => a.AccountBalance)
            .HasColumnType("decimal(18,2)");

        builder.Property(a => a.AccountType)
            .HasConversion(
                v => v.ToString().ToUpperInvariant(),
                v => Enum.Parse<AccountType>(v, ignoreCase: true))
            .HasColumnType("varchar(50)")
            .IsUnicode(false) // Ensures LINQ parameters are also sent as VARCHAR (because of has conversion)
            .IsRequired();

        builder.HasIndex(a => new { a.CustomerId, a.AccountType });

        builder.HasOne(a => a.Customer)
            .WithMany(c => c.Accounts)
            .HasForeignKey(a => a.CustomerId)
            .OnDelete(DeleteBehavior.Restrict); // Prevents accidental cascading deletes on money records
    }
}
