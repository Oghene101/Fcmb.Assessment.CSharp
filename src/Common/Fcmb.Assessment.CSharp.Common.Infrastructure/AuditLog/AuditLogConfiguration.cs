using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.AuditLog;

// Clustered on a monthly partition scheme over OccurredOn (see each module's migration), so the key includes it.
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => new { a.Id, a.OccurredOn });

        builder.Property(a => a.Action)
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(a => a.UserId)
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(a => a.EntityName)
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(a => a.EntityId)
            .IsRequired();

        builder.Property(a => a.OccurredOn)
            .IsRequired();

        builder.Property(a => a.Changes)
            .IsRequired();
    }
}
