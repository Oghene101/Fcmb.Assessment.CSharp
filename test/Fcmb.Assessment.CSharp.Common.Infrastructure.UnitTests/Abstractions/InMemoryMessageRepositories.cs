using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Application.Inbox;
using Fcmb.Assessment.CSharp.Common.Application.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.UnitTests.Abstractions;

// Stand-ins for the SQL Server repositories: reads come back untracked, as Dapper's do.
internal sealed class InMemoryOutboxMessageRepository(
    MessagingTestDbContext db) : IOutboxMessageRepository, IOutboxMessageConsumerRepository
{
    public Task<Guid[]> ClaimAsync(string lane, int limit, DateTimeOffset now, DateTimeOffset claimedUntil) =>
        throw new NotSupportedException("Claiming is SQL Server specific and covered end to end.");

    public Task<OutboxMessage?> GetUnprocessedByIdAsync(Guid id) =>
        db.Set<OutboxMessage>()
            .AsNoTracking()
            .SingleOrDefaultAsync(message => message.Id == id && message.ProcessedOn == null);

    public Task<int> DeleteProcessedBeforeAsync(DateTimeOffset cutoff) =>
        throw new NotSupportedException("Purging is SQL Server specific and covered end to end.");

    public Task<bool> OutboxMessageConsumerExistsAsync(Guid outboxMessageId, string name) =>
        db.Set<OutboxMessageConsumer>()
            .AnyAsync(consumer => consumer.OutboxMessageId == outboxMessageId && consumer.Name == name);
}

internal sealed class InMemoryInboxMessageRepository(
    MessagingTestDbContext db) : IInboxMessageRepository, IInboxMessageConsumerRepository
{
    public Task<Guid[]> ClaimAsync(string lane, int limit, DateTimeOffset now, DateTimeOffset claimedUntil) =>
        throw new NotSupportedException("Claiming is SQL Server specific and covered end to end.");

    public Task<InboxMessage?> GetUnprocessedByIdAsync(Guid id) =>
        db.Set<InboxMessage>()
            .AsNoTracking()
            .SingleOrDefaultAsync(message => message.Id == id && message.ProcessedOn == null);

    public Task<int> DeleteProcessedBeforeAsync(DateTimeOffset cutoff) =>
        throw new NotSupportedException("Purging is SQL Server specific and covered end to end.");

    public Task<bool> InboxMessageConsumerExistsAsync(Guid inboxMessageId, string name) =>
        db.Set<InboxMessageConsumer>()
            .AnyAsync(consumer => consumer.InboxMessageId == inboxMessageId && consumer.Name == name);
}
