using System.Reflection;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Customers;
using Fcmb.Assessment.CSharp.Modules.Transactions.Infrastructure;
using ApplicationAssemblyReference = Fcmb.Assessment.CSharp.Modules.Transactions.Application.AssemblyReference;
using PresentationAssemblyReference = Fcmb.Assessment.CSharp.Modules.Transactions.Presentation.AssemblyReference;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.ArchitectureTests.Abstractions;

public abstract class BaseTest
{
    protected static readonly Assembly DomainAssembly = typeof(Customer).Assembly;

    protected static readonly Assembly ApplicationAssembly = ApplicationAssemblyReference.Assembly;

    protected static readonly Assembly PresentationAssembly = PresentationAssemblyReference.Assembly;

    protected static readonly Assembly InfrastructureAssembly = typeof(TransactionsModule).Assembly;

    protected static readonly string ApplicationLayer = ApplicationAssembly.GetName().Name!;

    protected static readonly string PresentationLayer = PresentationAssembly.GetName().Name!;

    protected static readonly string InfrastructureLayer = InfrastructureAssembly.GetName().Name!;
}
