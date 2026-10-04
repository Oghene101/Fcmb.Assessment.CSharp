using Fcmb.Assessment.CSharp.Common.Application.Inbox;

namespace Fcmb.Assessment.CSharp.Common.Application.Data;

public interface IInboxMessageRepository
{
    Task<Guid[]> GetIdsAsync(int batchSize);
    Task<InboxMessage?> GetByIdAsync(Guid id);
}
