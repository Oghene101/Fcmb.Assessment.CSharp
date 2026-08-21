using Fcmb.Assessment.CSharp.Common.Application.Outbox;

namespace Fcmb.Assessment.CSharp.Common.Application.Data;

public interface IOutboxMessageRepository
{
    Task<Guid[]> GetIdsAsync(int batchSize);
    Task<OutboxMessage?> GetByIdAsync(Guid id);
}
