using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.UnitTests.Abstractions;

// Answers every event with the same outcome, or throws when given an exception, and counts the calls.
internal sealed class RecordingHandler(
    Result? outcome = null,
    Exception? thrown = null)
    : IDomainEventHandler<CustomerNotifiedDomainEvent>, IIntegrationEventHandler<CustomerRegisteredIntegrationEvent>
{
    public int Calls { get; private set; }

    public Task<Result> Handle(CustomerNotifiedDomainEvent domainEvent, CancellationToken cancellationToken) =>
        Answer();

    public Task<Result> Handle(CustomerRegisteredIntegrationEvent integrationEvent,
        CancellationToken cancellationToken) =>
        Answer();

    private Task<Result> Answer()
    {
        Calls++;

        if (thrown is not null)
        {
            throw thrown;
        }

        return Task.FromResult(outcome ?? Result.Success());
    }
}
