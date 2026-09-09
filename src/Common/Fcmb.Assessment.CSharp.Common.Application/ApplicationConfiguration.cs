using System.Reflection;
using Fcmb.Assessment.CSharp.Common.Application.Behaviors;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Fcmb.Assessment.CSharp.Common.Application;

public static class ApplicationConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplication(Assembly[] moduleAssemblies)
        {
            services.Scan(scan => scan.FromAssemblies(moduleAssemblies)
                .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.Decorate(typeof(IRequestHandler<,>), typeof(ValidationDecorator.RequestHandler<,>));
            services.Decorate(typeof(IRequestHandler<>), typeof(ValidationDecorator.RequestHandler<>));

            services.Decorate(typeof(IRequestHandler<,>), typeof(LoggingDecorator.RequestHandler<,>));
            services.Decorate(typeof(IRequestHandler<>), typeof(LoggingDecorator.RequestHandler<>));

            services.Scan(scan => scan.FromAssemblies(moduleAssemblies)
                .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.Scan(scan => scan.FromAssemblies(moduleAssemblies)
                .AddClasses(classes => classes.AssignableTo(typeof(IIntegrationEventHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.AddValidatorsFromAssemblies(moduleAssemblies, includeInternalTypes: true);

            return services;
        }
    }
}
