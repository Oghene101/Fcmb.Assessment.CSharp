using System.Text.Json.Serialization;

namespace Fcmb.Assessment.CSharp.Common.Domain;

// Type decides whether the outbox and inbox retry a failure; it is kept off the API wire.
public sealed record Error(string Code, string Message, [property: JsonIgnore] ErrorType Type)
{
    public static Error? None => null;

    public static Error Problem(string code, string message) =>
        new(code, message, ErrorType.Problem);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static Error Dependency(string code, string message) =>
        new(code, message, ErrorType.Dependency);
}
