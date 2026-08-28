namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Configurations;

public sealed record KeyCloakSettings
{
    public const string Path = "KeyCloak";

    public string BaseUrl { get; init; }
    public string ClientId { get; init; }
    public string ClientSecret { get; init; }
    public string HealthUrl { get; init; }
    public string AuthorizationUrl { get; init; }
    public string TokenUrl { get; init; }
}
