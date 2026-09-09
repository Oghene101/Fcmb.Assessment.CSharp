using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;
using Fcmb.Assessment.CSharp.Modules.Users.IntegrationEvents;
using MassTransit;
using DomainUserType = Fcmb.Assessment.CSharp.Modules.Users.Domain.Users.UserType;
using IntegrationUserType = Fcmb.Assessment.CSharp.Modules.Users.IntegrationEvents.UserType;

namespace Fcmb.Assessment.CSharp.Modules.Users.Application.Users;

#pragma warning disable S3928
#pragma warning disable CA2208
internal sealed class UserSignedUpDomainEventHandler(
    IPublishEndpoint publishEndpoint) : IDomainEventHandler<UserSignedUpDomainEvent>
{
    public async Task Handle(UserSignedUpDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        IntegrationUserType userType = domainEvent.UserType switch
        {
            DomainUserType.Individual => IntegrationUserType.Individual,
            DomainUserType.Corporate => IntegrationUserType.Corporate,
            _ => throw new ArgumentOutOfRangeException(
                nameof(domainEvent.UserType),
                domainEvent.UserType,
                "Unsupported domain user type.")
        };

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
                userType
            ), cancellationToken);
    }
}
#pragma warning restore CA2208
#pragma warning restore S3928
