using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;

namespace Fcmb.Assessment.CSharp.Modules.Users.Application.Users;

internal sealed class UserSignedUpDomainEventHandler : IDomainEventHandler<UserSignedUpDomainEvent>
{
    public async Task Handle(UserSignedUpDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
