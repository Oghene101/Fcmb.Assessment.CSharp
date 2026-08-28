using Microsoft.Extensions.DependencyInjection;

namespace Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Configurations;

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
