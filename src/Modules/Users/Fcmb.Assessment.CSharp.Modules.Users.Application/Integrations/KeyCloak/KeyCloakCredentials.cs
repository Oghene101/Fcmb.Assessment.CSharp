namespace Fcmb.Assessment.CSharp.Modules.Users.Application.Integrations.KeyCloak;

public sealed record KeyCloakCredentials(
    string Type,
    string Value,
    bool Temporary);
