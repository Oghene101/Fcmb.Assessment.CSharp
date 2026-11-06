using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.AuditLog;

public static class AuditLogPartitionsConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAuditLogPartitions<TUnitOfWork>(
            string module,
            string settingsPath)
            where TUnitOfWork : ICommonUnitOfWork
        {
            services.TryAddSingleton(new MessagingModule<TUnitOfWork>(module));

            services.AddOptions<AuditLogSettings>(module)
                .BindConfiguration(settingsPath)
                .Validate(settings => settings.RetentionMonths >= 0
                                      && settings.PartitionMonthsAhead >= 1
                                      && settings.PartitionJobIntervalMinutes > 0,
                    $"{settingsPath} needs a RetentionMonths of 0 or more, a PartitionMonthsAhead of at least 1 "
                    + "and a positive PartitionJobIntervalMinutes.")
                .ValidateOnStart();

            return services;
        }
    }
}
