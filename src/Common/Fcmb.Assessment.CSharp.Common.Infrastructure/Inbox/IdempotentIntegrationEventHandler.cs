using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Application.Inbox;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Inbox;

public sealed class IdempotentIntegrationEventHandler<TIntegrationEvent, TUnitOfWork>(
    IIntegrationEventHandler<TIntegrationEvent> decorated,
    TUnitOfWork uOw)
    : IIntegrationEventHandler<TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
    where TUnitOfWork : ICommonUnitOfWork
{
    public async Task<Result> Handle(TIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        string consumerName = decorated.GetType().Name;

        bool alreadyHandled = await uOw.InboxMessageConsumersReadRepository.InboxMessageConsumerExistsAsync(
            integrationEvent.Id,
            consumerName);

        if (alreadyHandled)
        {
            return Result.Success();
        }

        Result handled = await decorated.Handle(integrationEvent, cancellationToken);

        if (!handled.IsSuccess)
        {
            return handled;
        }

        await uOw.InboxMessageConsumersWriteRepository.AddAsync(
            new InboxMessageConsumer(
                integrationEvent.Id,
                consumerName),
            cancellationToken);

        await uOw.SaveChangesAsync(cancellationToken);

        return handled;
    }
}
