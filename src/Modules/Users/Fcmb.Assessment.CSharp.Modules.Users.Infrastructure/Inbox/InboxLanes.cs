using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Inbox;

internal static class InboxLanes
{
    public const string Default = MessageLanes.Default;

    public static IReadOnlyList<string> All { get; } = [Default];

    public static string Of(IIntegrationEvent integrationEvent) => Default;
}
