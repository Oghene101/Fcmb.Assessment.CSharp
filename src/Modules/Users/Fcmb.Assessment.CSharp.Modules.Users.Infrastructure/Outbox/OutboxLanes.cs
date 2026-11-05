using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Outbox;

// Events whose handlers publish to the broker get their own lane, so a broker outage holds up nothing else.
internal static class OutboxLanes
{
    public const string IntegrationEvents = "IntegrationEvents";
    public const string Default = MessageLanes.Default;

    public static IReadOnlyList<string> All { get; } = [IntegrationEvents, Default];

    public static string Of(IDomainEvent domainEvent) => domainEvent switch
    {
        UserSignedUpDomainEvent => IntegrationEvents,
        _ => Default
    };
}
