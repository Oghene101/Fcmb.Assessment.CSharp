using Fcmb.Assessment.CSharp.Common.Application.Inbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Inbox;

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    private const int QueueFillFactor = 80;

    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");

        builder.Property(a => a.Type)
            .HasColumnType("varchar(200)")
            .IsRequired();

        builder.Property(a => a.Lane)
            .HasColumnType("varchar(50)")
            .HasDefaultValue(MessageLanes.Default)
            .IsRequired();

        builder.Property(a => a.Content)
            .HasColumnType("nvarchar(4000)")
            .IsRequired();

        builder.Property(a => a.OccurredOn)
            .IsRequired();

        builder.Property(a => a.RetryCount)
            .IsRequired();

        // Unaffected by a claim, so a lease never moves the row in the index; retry times change only on failure.
        builder.Property(a => a.AvailableOn)
            .HasComputedColumnSql("COALESCE([NextRetryOn], [OccurredOn])", stored: true);

        // Claims update rows in place and retries move them, so pages keep room to absorb both without splitting.
        builder.HasKey(a => a.Id)
            .HasFillFactor(QueueFillFactor);

        // Ordered by availability, a claim stops at the first message not yet due instead of reading past every
        // message waiting on a retry.
        builder.HasIndex(a => new { a.Lane, a.AvailableOn })
            .HasFilter("[ProcessedOn] IS NULL")
            .HasFillFactor(QueueFillFactor)
            .HasDatabaseName("IX_InboxMessages_Unprocessed");

        builder.HasIndex(a => a.ProcessedOn)
            .HasFilter("[ProcessedOn] IS NOT NULL")
            .HasDatabaseName("IX_InboxMessages_Processed");
    }
}
