using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Application.Outbox;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Transactions.Application.Data;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Outbox;

internal sealed class IdempotentDomainEventHandler<TDomainEvent>(
    IDomainEventHandler<TDomainEvent> decorated,
    IUnitOfWork uOw)
    : IDomainEventHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    public async Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (await uOw.OutboxMessageConsumersReadRepository.OutboxMessageConsumerExistsAsync(domainEvent.Id,
                decorated.GetType().Name))
        {
            return;
        }

        await decorated.Handle(domainEvent, cancellationToken);

        await uOw.OutboxMessageConsumersWriteRepository.AddAsync(
            new OutboxMessageConsumer(
                domainEvent.Id,
                decorated.GetType().Name),
            cancellationToken);

        await uOw.SaveChangesAsync(cancellationToken);
    }
}
