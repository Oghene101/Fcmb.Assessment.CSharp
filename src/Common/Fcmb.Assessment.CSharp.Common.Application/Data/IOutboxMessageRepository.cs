using Fcmb.Assessment.CSharp.Common.Application.Outbox;

namespace Fcmb.Assessment.CSharp.Common.Application.Data;

public interface IOutboxMessageRepository
{
    Task<Guid[]> ClaimAsync(
        string lane,
        int limit,
        DateTimeOffset now,
        DateTimeOffset claimedUntil);

    Task<OutboxMessage?> GetUnprocessedByIdAsync(Guid id);

    Task<int> DeleteProcessedBeforeAsync(DateTimeOffset cutoff);
}
