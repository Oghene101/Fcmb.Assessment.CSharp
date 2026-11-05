using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Modules.Users.Application.Integrations.KeyCloak;

public static class KeyCloakErrors
{
    public static readonly Error EmailAlreadyExists =
        Error.Conflict("Identity.EmailAlreadyExists", "Identity email already exists");
}
