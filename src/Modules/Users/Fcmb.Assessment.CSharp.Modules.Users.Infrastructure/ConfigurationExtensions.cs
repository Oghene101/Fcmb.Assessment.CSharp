using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Integrations.KeyCloak;
using Microsoft.Extensions.DependencyInjection;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure;

internal static class ConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection AddUsersOptions()
        {
            services.AddOptions<KeyCloakSettings>()
                .BindConfiguration(KeyCloakSettings.Path)
                .ValidateOnStart();

            return services;
        }
    }
}
