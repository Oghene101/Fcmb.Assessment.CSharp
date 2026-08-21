using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.AuditLog;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        
        builder.Property(a => a.Action)
            .HasColumnType("varchar(20)")
            .IsRequired();

        builder.Property(a => a.UserId)
            .HasColumnType("varchar(50)")
            .IsRequired();
        
        builder.HasIndex(a => a.UserId);

        builder.Property(a => a.EntityName)
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(a => a.EntityId)
            .IsRequired();

        builder.Property(a => a.OccurredOn)
            .IsRequired();

        builder.Property(a => a.Changes)
            .IsRequired();
        
        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        
        builder.HasIndex(a => a.OccurredOn);
    }
}
