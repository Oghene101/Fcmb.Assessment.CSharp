using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Application.Outbox;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;

public sealed class InsertOutboxMessagesInterceptor<TUnitOfWork>(
    Func<IDomainEvent, string> laneOf,
    OutboxWakeUps<TUnitOfWork> wakeUps) : SaveChangesInterceptor
    where TUnitOfWork : ICommonUnitOfWork
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            InsertOutboxMessages(eventData.Context);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            WakeUpLanesOfNewMessages(eventData.Context);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    // A message being processed carries a lease, a retried one a retry time; only a new message has neither.
    private void WakeUpLanesOfNewMessages(DbContext context)
    {
        string[] lanesOfNewMessages =
        [
            .. context.ChangeTracker
                .Entries<OutboxMessage>()
                .Select(entry => entry.Entity)
                .Where(message => message.ProcessedOn == null
                                  && message.ClaimedUntil == null
                                  && message.NextRetryOn == null)
                .Select(message => message.Lane)
                .Distinct()
        ];

        foreach (string lane in lanesOfNewMessages)
        {
            wakeUps.WakeUp(lane);
        }
    }

    private void InsertOutboxMessages(DbContext context)
    {
        OutboxMessage[] outboxMessages = context
            .ChangeTracker
            .Entries<Entity>()
            .Select(entry => entry.Entity)
            .SelectMany(entity =>
            {
                IReadOnlyCollection<IDomainEvent> domainEvents = [.. entity.DomainEvents];

                entity.ClearDomainEvents();

                return domainEvents;
            })
            .Select(domainEvent => new OutboxMessage
            {
                Id = domainEvent.Id,
                Type = domainEvent.GetType().Name,
                Lane = laneOf(domainEvent),
                Content = JsonConvert.SerializeObject(domainEvent, SerializerSettings.Instance),
                OccurredOn = domainEvent.OccurredOn
            })
            .ToArray();

        if (outboxMessages.Length is 0)
        {
            return;
        }

        context.Set<OutboxMessage>().AddRange(outboxMessages);
    }
}
