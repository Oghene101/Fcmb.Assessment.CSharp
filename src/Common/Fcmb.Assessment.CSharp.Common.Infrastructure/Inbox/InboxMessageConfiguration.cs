using Fcmb.Assessment.CSharp.Common.Application.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Inbox;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");

        builder.Property(a => a.Type)
            .HasColumnType("varchar(200)")
            .IsRequired();

        builder.Property(a => a.Content)
            .HasColumnType("nvarchar(4000)")
            .IsRequired();

        builder.Property(a => a.OccurredOn)
            .IsRequired();

        builder.Property(a => a.RetryCount)
            .IsRequired();
    }
}
