using Fcmb.Assessment.CSharp.ArchitectureTests.Abstractions;
using NetArchTest.Rules;

namespace Fcmb.Assessment.CSharp.ArchitectureTests.Layers;

public sealed class ModuleTests : BaseTest
{
    [Fact]
    public void UsersModule_ShouldNotHaveDependencyOn_AnyOtherModule()
    {
        Types.InAssemblies(UsersAssemblies)
            .Should()
            .NotHaveDependencyOn(TransactionsNamespace)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void TransactionsModule_ShouldDependOnTheUsersModule_OnlyThroughItsIntegrationEvents()
    {
        string[] usersInternals =
        [
            UsersDomainNamespace,
            UsersApplicationNamespace,
            UsersPresentationNamespace,
            UsersInfrastructureNamespace
        ];

        Types.InAssemblies(TransactionsAssemblies)
            .Should()
            .NotHaveDependencyOnAny(usersInternals)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void CommonProjects_ShouldNotHaveDependencyOn_AnyModule()
    {
        Types.InAssemblies(CommonAssemblies)
            .Should()
            .NotHaveDependencyOn(ModulesNamespace)
            .GetResult()
            .ShouldBeSuccessful();
    }
}
