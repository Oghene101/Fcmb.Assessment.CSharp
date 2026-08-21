using Microsoft.Extensions.DependencyInjection;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Authentication;

internal static class AuthenticationExtensions
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection AddAuthenticationInternal()
        {
            services.AddAuthorization();

            services.AddAuthentication().AddJwtBearer();

            services.AddHttpContextAccessor();

            services.ConfigureOptions<JwtBearerConfigureOptions>();

            return services;
        }
    }
}
