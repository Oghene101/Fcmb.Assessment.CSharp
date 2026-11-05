using Fcmb.Assessment.CSharp.Common.Application.Inbox;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Serialization;
using Fcmb.Assessment.CSharp.Modules.Users.Application.Data;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Inbox;

internal sealed class IntegrationEventConsumer<TIntegrationEvent>(
    IUnitOfWork uOw,
    ILogger<IntegrationEventConsumer<TIntegrationEvent>> logger)
    : IConsumer<TIntegrationEvent>
    where TIntegrationEvent : class, IIntegrationEvent
{
    private const int PrimaryKeyViolation = 2627;

    public async Task Consume(ConsumeContext<TIntegrationEvent> context)
    {
        TIntegrationEvent integrationEvent = context.Message;

        var inboxMessage = new InboxMessage
        {
            Id = integrationEvent.Id,
            Type = integrationEvent.GetType().Name,
            Lane = InboxLanes.Of(integrationEvent),
            Content = JsonConvert.SerializeObject(integrationEvent, SerializerSettings.Instance),
            OccurredOn = integrationEvent.OccurredOn
        };

        await uOw.InboxMessagesWriteRepository.AddAsync(inboxMessage, context.CancellationToken);

        // The event id is the inbox key, so a redelivery of a stored event is acknowledged rather than stored twice.
        try
        {
            await uOw.SaveChangesAsync(context.CancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: PrimaryKeyViolation })
        {
            logger.LogInformation(exception, "Inbox already holds {EventType} {MessageId}; acknowledging the redelivery",
                inboxMessage.Type, inboxMessage.Id);
        }
    }
}
