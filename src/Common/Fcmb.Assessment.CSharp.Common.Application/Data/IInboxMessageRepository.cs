using Fcmb.Assessment.CSharp.Common.Application.Inbox;

namespace Fcmb.Assessment.CSharp.Common.Application.Data;

public interface IInboxMessageRepository
{
    Task<Guid[]> ClaimAsync(
        string lane,
        int limit,
        DateTimeOffset now,
        DateTimeOffset claimedUntil);

    Task<InboxMessage?> GetUnprocessedByIdAsync(Guid id);

    Task<int> DeleteProcessedBeforeAsync(DateTimeOffset cutoff);
}
