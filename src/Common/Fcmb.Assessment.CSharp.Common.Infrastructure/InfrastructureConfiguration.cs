using Fcmb.Assessment.CSharp.Common.Infrastructure.AuditLog;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Authentication;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Configurations;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Quartz;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure;

public static class InfrastructureConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInfrastructure()
        {
            services.AddOptionsInternal();

            services.AddAuthenticationInternal();

            services.AddSingleton(TimeProvider.System);

            services.TryAddSingleton<InsertOutboxMessagesInterceptor>();
            services.AddScoped<InsertAuditLogsInterceptor>();

            services.AddQuartz();
            services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

            return services;
        }
    }
}
