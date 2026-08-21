using System.Globalization;
using System.Text;
using Fcmb.Assessment.CSharp.Common.Application.Authorization;
using Fcmb.Assessment.CSharp.Common.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.AuditLog;

public sealed class InsertAuditLogsInterceptor(
    IClaimsProvider auth) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            InsertAuditLogs(eventData.Context);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void InsertAuditLogs(DbContext context)
    {
        IEnumerable<EntityEntry<Entity>> entries =
        [
            .. context.ChangeTracker
                .Entries<Entity>()
                .Where(e => e.State is EntityState.Added
                    or EntityState.Modified
                    or EntityState.Deleted)
        ];

        string userId;
        try
        {
            userId = auth.GetUserId();
        }
        catch (UnauthorizedAccessException)
        {
            userId = "system";
        }

        List<AuditLog> auditLogs =
        [
            .. entries.Select(entry => new AuditLog
            {
                Action = entry.State.ToString(),
                UserId = userId,
                EntityName = entry.Metadata.DisplayName(),
                EntityId = entry.Entity.Id,
                OccurredOn = DateTimeOffset.UtcNow,
                Changes = GetChanges(entry)
            })

        ];

        if (auditLogs.Count is 0)
        {
            return;
        }

        context.Set<AuditLog>().AddRange(auditLogs);
    }

    private static string GetChanges(EntityEntry<Entity> entry) =>
        entry.State switch
        {
            EntityState.Added => $"{entry.Metadata.DisplayName()} entity created",
            EntityState.Deleted => $"{entry.Metadata.DisplayName()} entity deleted",
            _ => BuildChangeDiff(entry)
        };

    private static string BuildChangeDiff(EntityEntry<Entity> entry)
    {
        var changes = new StringBuilder();
        foreach (IProperty property in entry.OriginalValues.Properties)
        {
            object? originalValue = entry.OriginalValues[property];
            object? currentValue = entry.CurrentValues[property];
            if (!Equals(originalValue, currentValue))
            {
                changes.AppendLine(
                    string.Format(CultureInfo.InvariantCulture,
                        "{0}: from '{1}' to '{2}'",
                        property.Name, originalValue, currentValue));
            }
        }

        return changes.ToString();
    }
}
