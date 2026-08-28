using Refit;

namespace Fcmb.Assessment.CSharp.Modules.Users.Application.Integrations.KeyCloak;

public interface IKeyCloakClient
{
    [Post("/admin/realms/fcmb-assessment-csharp/users")]
    Task<HttpResponseMessage> CreateUserAsync(KeyCloakCreateUserRequest request);
}
