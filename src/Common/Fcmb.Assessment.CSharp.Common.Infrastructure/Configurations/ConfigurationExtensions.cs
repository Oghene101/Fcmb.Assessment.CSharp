using Microsoft.Extensions.DependencyInjection;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Configurations;

internal static class ConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection AddOptionsInternal()
        {
            services.AddOptions<AuthenticationSettings>()
                .BindConfiguration(AuthenticationSettings.Path)
                .ValidateOnStart();

            return services;
        }
    }
}
