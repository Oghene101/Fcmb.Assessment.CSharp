using Fcmb.Assessment.CSharp.Common.Application.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;

internal sealed class DeadLetteredOutboxMessageConfiguration : IEntityTypeConfiguration<DeadLetteredOutboxMessage>
{
    public void Configure(EntityTypeBuilder<DeadLetteredOutboxMessage> builder)
    {
        builder.ToTable("DeadLetteredOutboxMessages");
        
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

