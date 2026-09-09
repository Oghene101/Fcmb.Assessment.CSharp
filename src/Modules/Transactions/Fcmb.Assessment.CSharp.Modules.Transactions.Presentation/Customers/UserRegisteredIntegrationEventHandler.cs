using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Modules.Users.IntegrationEvents;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Presentation.Customers;

internal sealed class UserRegisteredIntegrationEventHandler
    : IIntegrationEventHandler<UserSignedUpIntegrationEvent>
{
    public async Task Handle(UserSignedUpIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
