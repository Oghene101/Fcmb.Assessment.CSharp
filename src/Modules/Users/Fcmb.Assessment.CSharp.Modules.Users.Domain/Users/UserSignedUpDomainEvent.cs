using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;

public sealed record UserSignedUpDomainEvent(
    Guid Id,
    DateTimeOffset OccurredOn) : IDomainEvent;
