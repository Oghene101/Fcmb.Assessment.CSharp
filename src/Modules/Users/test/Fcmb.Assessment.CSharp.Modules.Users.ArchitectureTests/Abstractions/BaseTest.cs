using System.Reflection;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure;
using Fcmb.Assessment.CSharp.Modules.Users.IntegrationEvents;
using ApplicationAssemblyReference = Fcmb.Assessment.CSharp.Modules.Users.Application.AssemblyReference;
using PresentationAssemblyReference = Fcmb.Assessment.CSharp.Modules.Users.Presentation.AssemblyReference;

namespace Fcmb.Assessment.CSharp.Modules.Users.ArchitectureTests.Abstractions;

public abstract class BaseTest
{
    protected static readonly Assembly DomainAssembly = typeof(User).Assembly;

    protected static readonly Assembly ApplicationAssembly = ApplicationAssemblyReference.Assembly;

    protected static readonly Assembly PresentationAssembly = PresentationAssemblyReference.Assembly;

    protected static readonly Assembly InfrastructureAssembly = typeof(UsersModule).Assembly;

    protected static readonly Assembly IntegrationEventsAssembly = typeof(UserSignedUpIntegrationEvent).Assembly;

    protected static readonly string DomainLayer = DomainAssembly.GetName().Name!;

    protected static readonly string ApplicationLayer = ApplicationAssembly.GetName().Name!;

    protected static readonly string PresentationLayer = PresentationAssembly.GetName().Name!;

    protected static readonly string InfrastructureLayer = InfrastructureAssembly.GetName().Name!;
}
