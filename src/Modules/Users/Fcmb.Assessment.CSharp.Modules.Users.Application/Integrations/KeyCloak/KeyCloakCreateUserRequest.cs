namespace Fcmb.Assessment.CSharp.Modules.Users.Application.Integrations.KeyCloak;

public sealed record KeyCloakCreateUserRequest(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    bool EmailVerified,
    bool Enabled,
    KeyCloakCredentials[] Credentials);
