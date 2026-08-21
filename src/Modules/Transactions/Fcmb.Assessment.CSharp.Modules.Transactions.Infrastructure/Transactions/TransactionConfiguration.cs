using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Transactions;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.Property(t => t.TransactionType)
            .HasConversion(
                v => v.ToString().ToUpperInvariant(),
                v => Enum.Parse<TransactionType>(v, ignoreCase: true))
            .HasColumnType("varchar(50)")
            .IsUnicode(false) // Ensures LINQ parameters are also sent as VARCHAR (because of has conversion)
            .IsRequired();

        builder.Property(t => t.AccountNumber)
            .HasColumnType("char(10)")
            .IsRequired();

        builder.HasIndex(t => new { t.AccountNumber, t.CreatedAt });

        builder.Property(t => t.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(t => t.DiscountedAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(a => a.Rate)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
    }
}
