using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;
using Fcmb.Assessment.CSharp.Modules.Users.IntegrationEvents;
using MassTransit;
using DomainUserType = Fcmb.Assessment.CSharp.Modules.Users.Domain.Users.UserType;
using IntegrationUserType = Fcmb.Assessment.CSharp.Modules.Users.IntegrationEvents.UserType;

namespace Fcmb.Assessment.CSharp.Modules.Users.Application.Users;

internal sealed class UserSignedUpDomainEventHandler(
    IPublishEndpoint publishEndpoint) : IDomainEventHandler<UserSignedUpDomainEvent>
{
    public async Task<Result> Handle(UserSignedUpDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        IntegrationUserType? userType = domainEvent.UserType switch
        {
            DomainUserType.Individual => IntegrationUserType.Individual,
            DomainUserType.Corporate => IntegrationUserType.Corporate,
            _ => null
        };

        if (userType is null)
        {
            return UserErrors.UnsupportedUserType(domainEvent.UserType);
        }

        await publishEndpoint.Publish(
            new UserSignedUpIntegrationEvent(
                domainEvent.Id,
                domainEvent.OccurredOn,
                domainEvent.UserId,
                domainEvent.FirstName,
                domainEvent.LastName,
                domainEvent.Email,
                domainEvent.PhoneNumber,
                domainEvent.Dob,
                userType.Value
            ), cancellationToken);

        return Result.Success();
    }
}
