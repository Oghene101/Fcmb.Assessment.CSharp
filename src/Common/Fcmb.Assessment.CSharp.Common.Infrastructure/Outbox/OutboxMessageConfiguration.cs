using Fcmb.Assessment.CSharp.Common.Application.Outbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

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

        builder.HasIndex(a => new { a.Lane, a.OccurredOn })
            .HasFilter("[ProcessedOn] IS NULL")
            .HasDatabaseName("IX_OutboxMessages_Unprocessed");
    }
}
