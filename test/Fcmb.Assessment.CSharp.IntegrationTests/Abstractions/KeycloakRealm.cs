namespace Fcmb.Assessment.CSharp.IntegrationTests.Abstractions;

// Mirrors .files/fcmb-assessment-csharp-realm-export.json, which the Keycloak container imports at start-up.
internal static class KeycloakRealm
{
    internal const string Name = "fcmb-assessment-csharp";

    internal const string AdminClientId = "fcmb-assessment-csharp-admin";

    internal const string AdminClientSecret = "fcmb-assessment-csharp-admin-test-secret";

    internal const string PublicClientId = "fcmb-assessment-csharp-public";

    internal const string ExportFileName = "fcmb-assessment-csharp-realm-export.json";

    internal const string ImportPath = "/opt/keycloak/data/import/realm.json";
}
