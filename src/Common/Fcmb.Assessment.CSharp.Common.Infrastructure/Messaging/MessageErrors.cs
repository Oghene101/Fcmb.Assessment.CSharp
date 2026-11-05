using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;

public static class MessageErrors
{
    // An exception says nothing about whether a retry can succeed, so it is retried like a dependency outage.
    public static Error HandlerThrew(Exception exception) => Error.Dependency(
        "Message.HandlerThrew",
        exception.ToString());

    public static Error Unreadable(Exception exception) => Error.Problem(
        "Message.Unreadable",
        exception.Message);
}
