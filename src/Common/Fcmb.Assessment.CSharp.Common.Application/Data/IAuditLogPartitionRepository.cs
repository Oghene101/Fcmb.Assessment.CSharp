namespace Fcmb.Assessment.CSharp.Common.Application.Data;

public interface IAuditLogPartitionRepository
{
    Task CreateMonthsAheadAsync(
        DateTimeOffset currentMonth,
        int monthsAhead);

    /// <summary>Empties and merges away every month wholly before <paramref name="keepFrom"/>; returns how many.</summary>
    Task<int> DropMonthsBeforeAsync(DateTimeOffset keepFrom);
}
