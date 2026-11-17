using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;

public static class MessageErrors
{
    // An exception says nothing about whether a retry can succeed, so it is retried like a dependency outage.
    public static Error HandlerThrew(Exception exception) => Error.Dependency(
        "Message.HandlerThrew",
        exception.ToString());

    // The handlers' work and the processed message are saved together; failing that, the handlers run again later.
    public static Error SaveFailed(Exception exception) => Error.Dependency(
        "Message.SaveFailed",
        exception.ToString());

    public static Error Unreadable(Exception exception) => Error.Problem(
        "Message.Unreadable",
        exception.Message);
}
