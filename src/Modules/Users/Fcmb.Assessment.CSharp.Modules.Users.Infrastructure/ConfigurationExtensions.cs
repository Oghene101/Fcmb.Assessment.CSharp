using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Inbox;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Integrations.KeyCloak;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure.Outbox;
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

            services.AddOptions<OutboxSettings>()
                .BindConfiguration(OutboxSettings.Path)
                .ValidateOnStart();

            services.AddOptions<InboxSettings>()
                .BindConfiguration(InboxSettings.Path)
                .ValidateOnStart();

            return services;
        }
    }
}
