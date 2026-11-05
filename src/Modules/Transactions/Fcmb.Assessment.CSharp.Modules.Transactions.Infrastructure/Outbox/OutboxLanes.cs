using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure.Outbox;

internal static class OutboxLanes
{
    public const string Default = MessageLanes.Default;

    public static IReadOnlyList<string> All { get; } = [Default];

    public static string Of(IDomainEvent domainEvent) => Default;
}
