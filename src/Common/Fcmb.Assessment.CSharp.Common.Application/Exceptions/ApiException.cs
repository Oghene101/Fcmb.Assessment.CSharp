using System.Net;
using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Common.Application.Exceptions;

public sealed class ApiException(
    string title,
    string message,
    Error error,
    HttpStatusCode statusCode) : Exception(message)
{
    public string Title => title;

    public Dictionary<string, object?> Extensions => new() { ["error"] = error };

    public int StatusCode => (int)statusCode;

    public static ApiException NotFound(Error error, string message = "The requested resource was not found.")
        => new("Not Found", message, error, HttpStatusCode.NotFound);

    public static ApiException BadRequest(Error error, string message = "Invalid request parameters.")
        => new("Bad Request", message, error, HttpStatusCode.BadRequest);

    public static ApiException Conflict(Error error,
        string message = "A conflict occurred with the current state of the resource.")
        => new("Conflict", message, error, HttpStatusCode.Conflict);

    public static ApiException Unauthorized(Error error, string message = "Authentication required.")
        => new("Unauthorized", message, error, HttpStatusCode.Unauthorized);

    public static ApiException Forbidden(Error error, string message = "Insufficient permissions.")
        => new("Forbidden", message, error, HttpStatusCode.Forbidden);
}
