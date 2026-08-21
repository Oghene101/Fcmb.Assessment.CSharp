using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Customers;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Rewards;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Rewards;

internal sealed class RewardConfiguration : IEntityTypeConfiguration<Reward>
{
    public void Configure(EntityTypeBuilder<Reward> builder)
    {
        builder.Property(r => r.Points)
            .IsRequired();

        builder.Property(r => r.QualifyingTransfers)
            .IsRequired();

        builder.Property(r => r.LastResetDate)
            .IsRequired();

        builder.HasOne<Customer>()
            .WithOne(c => c.Reward)
            .HasForeignKey<Reward>(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.CustomerId)
            .IsUnique();
    }
}
