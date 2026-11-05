using System.Reflection;
using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Inbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;

public static class MessagingConfiguration
{
    extension(IServiceCollection services)
    {
        // Decorates the module's domain event handlers and runs one processor per outbox lane.
        public IServiceCollection AddOutbox<TUnitOfWork>(
            string module,
            string settingsPath,
            IReadOnlyList<string> lanes,
            Assembly handlersAssembly)
            where TUnitOfWork : ICommonUnitOfWork
        {
            services.TryAddSingleton(new MessagingModule<TUnitOfWork>(module));

            string requiredLanes = string.Join(", ", lanes);

            services.AddOptions<OutboxSettings>(module)
                .BindConfiguration(settingsPath)
                .Validate(settings => settings.LeaseSeconds > 0
                                      && settings.RetentionDays > 0
                                      && settings.PurgeIntervalMinutes > 0,
                    $"{settingsPath}:LeaseSeconds, RetentionDays and PurgeIntervalMinutes must be positive.")
                .Validate(settings => HasEveryLane(settings.Lanes, lanes),
                    $"{settingsPath}:Lanes needs {requiredLanes}, each with a positive Concurrency, "
                    + "PollIntervalMilliseconds and MaxRetries.")
                .ValidateOnStart();

            services.DecorateHandlers(
                typeof(IDomainEventHandler<>),
                typeof(IdempotentDomainEventHandler<,>),
                typeof(TUnitOfWork),
                handlersAssembly);

            services.AddSingleton<OutboxMessageProcessor<TUnitOfWork>>();

            foreach (string lane in lanes)
            {
                services.AddSingleton<IHostedService>(serviceProvider =>
                {
                    OutboxSettings outbox = serviceProvider
                        .GetRequiredService<IOptionsMonitor<OutboxSettings>>()
                        .Get(module);

                    var messageLane = new MessageLane(module, lane, outbox.Lanes[lane], outbox.LeaseSeconds);

                    return ActivatorUtilities.CreateInstance<OutboxLaneProcessor<TUnitOfWork>>(
                        serviceProvider,
                        messageLane);
                });
            }

            return services;
        }

        // Decorates the module's integration event handlers and runs one processor per inbox lane.
        public IServiceCollection AddInbox<TUnitOfWork>(
            string module,
            string settingsPath,
            IReadOnlyList<string> lanes,
            Assembly handlersAssembly)
            where TUnitOfWork : ICommonUnitOfWork
        {
            services.TryAddSingleton(new MessagingModule<TUnitOfWork>(module));

            string requiredLanes = string.Join(", ", lanes);

            services.AddOptions<InboxSettings>(module)
                .BindConfiguration(settingsPath)
                .Validate(settings => settings.LeaseSeconds > 0
                                      && settings.RetentionDays > 0
                                      && settings.PurgeIntervalMinutes > 0,
                    $"{settingsPath}:LeaseSeconds, RetentionDays and PurgeIntervalMinutes must be positive.")
                .Validate(settings => HasEveryLane(settings.Lanes, lanes),
                    $"{settingsPath}:Lanes needs {requiredLanes}, each with a positive Concurrency, "
                    + "PollIntervalMilliseconds and MaxRetries.")
                .ValidateOnStart();

            services.DecorateHandlers(
                typeof(IIntegrationEventHandler<>),
                typeof(IdempotentIntegrationEventHandler<,>),
                typeof(TUnitOfWork),
                handlersAssembly);

            services.AddSingleton<InboxMessageProcessor<TUnitOfWork>>();

            foreach (string lane in lanes)
            {
                services.AddSingleton<IHostedService>(serviceProvider =>
                {
                    InboxSettings inbox = serviceProvider
                        .GetRequiredService<IOptionsMonitor<InboxSettings>>()
                        .Get(module);

                    var messageLane = new MessageLane(module, lane, inbox.Lanes[lane], inbox.LeaseSeconds);

                    return ActivatorUtilities.CreateInstance<InboxLaneProcessor<TUnitOfWork>>(
                        serviceProvider,
                        messageLane);
                });
            }

            return services;
        }

        private void DecorateHandlers(
            Type openHandlerType,
            Type openIdempotentHandlerType,
            Type unitOfWorkType,
            Assembly handlersAssembly)
        {
            Type[] handlerServiceTypes = services
                .Where(sd => sd.ServiceType.IsGenericType &&
                             sd.ServiceType.GetGenericTypeDefinition() == openHandlerType &&
                             sd.ImplementationType != null &&
                             sd.ImplementationType.Assembly == handlersAssembly)
                .Select(sd => sd.ServiceType)
                .Distinct()
                .ToArray();

            foreach (Type handlerServiceType in handlerServiceTypes)
            {
                Type handledEvent = handlerServiceType
                    .GetGenericArguments()
                    .Single();

                Type closedIdempotentHandler = openIdempotentHandlerType.MakeGenericType(handledEvent, unitOfWorkType);

                services.Decorate(handlerServiceType, closedIdempotentHandler);
            }
        }
    }

    private static bool HasEveryLane(
        Dictionary<string, MessageLaneSettings> configuredLanes,
        IReadOnlyList<string> requiredLanes) =>
        requiredLanes.All(lane =>
        {
            MessageLaneSettings? laneSettings = configuredLanes.GetValueOrDefault(lane);

            return laneSettings is { Concurrency: > 0, PollIntervalMilliseconds: > 0, MaxRetries: > 0 }
                   && laneSettings.RetryDelaySeconds >= 0;
        });
}
