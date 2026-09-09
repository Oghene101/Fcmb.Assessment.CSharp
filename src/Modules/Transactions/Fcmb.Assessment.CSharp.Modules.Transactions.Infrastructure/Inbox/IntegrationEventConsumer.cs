using Fcmb.Assessment.CSharp.Common.Application.Inbox;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Serialization;
using Fcmb.Assessment.CSharp.Modules.Transactions.Application.Data;
using MassTransit;
using Newtonsoft.Json;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Inbox;

internal sealed class IntegrationEventConsumer<TIntegrationEvent>(
    IUnitOfWork uOw)
    : IConsumer<TIntegrationEvent>
    where TIntegrationEvent : class, IIntegrationEvent
{
    public async Task Consume(ConsumeContext<TIntegrationEvent> context)
    {
        TIntegrationEvent integrationEvent = context.Message;

        var inboxMessage = new InboxMessage
        {
            Id = integrationEvent.Id,
            Type = integrationEvent.GetType().Name,
            Content = JsonConvert.SerializeObject(integrationEvent, SerializerSettings.Instance),
            OccurredOn = integrationEvent.OccurredOn
        };

        await uOw.InboxMessagesWriteRepository.AddAsync(inboxMessage, context.CancellationToken);
        await uOw.SaveChangesAsync(context.CancellationToken);
    }
}
