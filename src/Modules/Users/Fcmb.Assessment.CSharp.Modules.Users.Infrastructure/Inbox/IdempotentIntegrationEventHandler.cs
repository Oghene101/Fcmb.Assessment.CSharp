using Fcmb.Assessment.CSharp.Common.Application.Inbox;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Data;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Inbox;

internal sealed class IdempotentIntegrationEventHandler<TIntegrationEvent>(
    IIntegrationEventHandler<TIntegrationEvent> decorated,
    IUnitOfWork uOw)
    : IIntegrationEventHandler<TIntegrationEvent>
    where TIntegrationEvent : IIntegrationEvent
{
    public async Task Handle(TIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        if (await uOw.InboxMessageConsumersReadRepository.InboxMessageConsumerExistsAsync(integrationEvent.Id,
                decorated.GetType().Name))
        {
            return;
        }

        await decorated.Handle(integrationEvent, cancellationToken);

        await uOw.InboxMessageConsumersWriteRepository.AddAsync(
            new InboxMessageConsumer(
                integrationEvent.Id,
                decorated.GetType().Name),
            cancellationToken);

        await uOw.SaveChangesAsync(cancellationToken);
    }
}
