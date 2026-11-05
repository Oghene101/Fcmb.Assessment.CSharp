using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Application.Outbox;
using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;

public sealed class IdempotentDomainEventHandler<TDomainEvent, TUnitOfWork>(
    IDomainEventHandler<TDomainEvent> decorated,
    TUnitOfWork uOw)
    : IDomainEventHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
    where TUnitOfWork : ICommonUnitOfWork
{
    public async Task<Result> Handle(TDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        string consumerName = decorated.GetType().Name;

        bool alreadyHandled = await uOw.OutboxMessageConsumersReadRepository.OutboxMessageConsumerExistsAsync(
            domainEvent.Id,
            consumerName);

        if (alreadyHandled)
        {
            return Result.Success();
        }

        Result handled = await decorated.Handle(domainEvent, cancellationToken);

        if (!handled.IsSuccess)
        {
            return handled;
        }

        await uOw.OutboxMessageConsumersWriteRepository.AddAsync(
            new OutboxMessageConsumer(
                domainEvent.Id,
                consumerName),
            cancellationToken);

        await uOw.SaveChangesAsync(cancellationToken);

        return handled;
    }
}
