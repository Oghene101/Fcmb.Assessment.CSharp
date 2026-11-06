namespace Fcmb.Assessment.CSharp.Common.Infrastructure.AuditLog;

public sealed record AuditLogSettings
{
    /// <summary>0 keeps every month.</summary>
    public int RetentionMonths { get; init; }

    public int PartitionMonthsAhead { get; init; }

    /// <summary>How often months are created ahead and old ones dropped.</summary>
    public int PartitionJobIntervalMinutes { get; init; }
}
