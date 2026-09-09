using Fcmb.Assessment.CSharp.Common.Application.Messaging;

namespace Fcmb.Assessment.CSharp.Modules.Users.IntegrationEvents;

public sealed record UserSignedUpIntegrationEvent(
    Guid Id,
    DateTimeOffset OccurredOn,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string PhoneNumber,
    DateOnly Dob,
    UserType UserType) : IIntegrationEvent;
