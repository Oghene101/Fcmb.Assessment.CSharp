using System.Reflection;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Common.Presentation;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Customers;
using Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;
using Fcmb.Assessment.CSharp.Modules.Users.Infrastructure;
using Fcmb.Assessment.CSharp.Modules.Users.IntegrationEvents;
using CommonInfrastructureAssemblyReference = Fcmb.Assessment.CSharp.Common.Infrastructure.AssemblyReference;
using TransactionsApplicationAssemblyReference =
    Fcmb.Assessment.CSharp.Modules.Transactions.Application.AssemblyReference;
using TransactionsPresentationAssemblyReference =
    Fcmb.Assessment.CSharp.Modules.Transactions.Presentation.AssemblyReference;
using UsersApplicationAssemblyReference = Fcmb.Assessment.CSharp.Modules.Users.Application.AssemblyReference;
using UsersPresentationAssemblyReference = Fcmb.Assessment.CSharp.Modules.Users.Presentation.AssemblyReference;

namespace Fcmb.Assessment.CSharp.ArchitectureTests.Abstractions;

public abstract class BaseTest
{
    protected const string ModulesNamespace = "Fcmb.Assessment.CSharp.Modules";

    protected const string UsersDomainNamespace = "Fcmb.Assessment.CSharp.Modules.Users.Domain";
    protected const string UsersApplicationNamespace = "Fcmb.Assessment.CSharp.Modules.Users.Application";
    protected const string UsersPresentationNamespace = "Fcmb.Assessment.CSharp.Modules.Users.Presentation";
    protected const string UsersInfrastructureNamespace = "Fcmb.Assessment.CSharp.Modules.Users.Infrastructure";

    protected const string TransactionsNamespace = "Fcmb.Assessment.CSharp.Modules.Transactions";

    protected const string CommonApplicationNamespace = "Fcmb.Assessment.CSharp.Common.Application";
    protected const string CommonPresentationNamespace = "Fcmb.Assessment.CSharp.Common.Presentation";
    protected const string CommonInfrastructureNamespace = "Fcmb.Assessment.CSharp.Common.Infrastructure";

    protected static readonly Assembly[] UsersAssemblies =
    [
        typeof(User).Assembly,
        UsersApplicationAssemblyReference.Assembly,
        UsersPresentationAssemblyReference.Assembly,
        typeof(UsersModule).Assembly,
        typeof(UserSignedUpIntegrationEvent).Assembly
    ];

    protected static readonly Assembly[] TransactionsAssemblies =
    [
        typeof(Customer).Assembly,
        TransactionsApplicationAssemblyReference.Assembly,
        TransactionsPresentationAssemblyReference.Assembly,
        typeof(TransactionsModule).Assembly
    ];

    protected static readonly Assembly CommonDomainAssembly = typeof(Entity).Assembly;

    protected static readonly Assembly CommonApplicationAssembly = typeof(ICommand).Assembly;

    protected static readonly Assembly CommonPresentationAssembly = typeof(IEndpoint).Assembly;

    protected static readonly Assembly CommonInfrastructureAssembly = CommonInfrastructureAssemblyReference.Assembly;

    protected static readonly Assembly[] CommonAssemblies =
    [
        CommonDomainAssembly,
        CommonApplicationAssembly,
        CommonPresentationAssembly,
        CommonInfrastructureAssembly
    ];
}
