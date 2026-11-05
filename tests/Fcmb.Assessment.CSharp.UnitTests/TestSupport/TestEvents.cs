using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.UnitTests.TestSupport;

public sealed record CustomerNotifiedDomainEvent(
    Guid Id,
    DateTimeOffset OccurredOn) : IDomainEvent;

public sealed record CustomerRegisteredIntegrationEvent(
    Guid Id,
    DateTimeOffset OccurredOn) : IIntegrationEvent;
