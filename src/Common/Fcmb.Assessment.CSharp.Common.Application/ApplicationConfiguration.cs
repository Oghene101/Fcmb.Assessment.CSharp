using Fcmb.Assessment.CSharp.Common.Application.Behaviors;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Fcmb.Assessment.CSharp.Common.Application;

public static class ApplicationConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplication()
        {
            services.Scan(scan => scan.FromAssembliesOf(typeof(ApplicationConfiguration))
                .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
                .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<,>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.Decorate(typeof(IRequestHandler<,>), typeof(ValidationDecorator.RequestHandler<,>));
            services.Decorate(typeof(IRequestHandler<>), typeof(ValidationDecorator.RequestHandler<>));

            // services.Decorate(typeof(IRequestHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));
            // services.Decorate(typeof(IRequestHandler<>), typeof(LoggingDecorator.CommandBaseHandler<>));

            services.Scan(scan => scan.FromAssembliesOf(typeof(ApplicationConfiguration))
                .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)), publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.AddValidatorsFromAssembly(typeof(ApplicationConfiguration).Assembly, includeInternalTypes: true);

            return services;
        }
    }
}
