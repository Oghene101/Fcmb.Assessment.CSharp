using System.Reflection;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Transactions.ArchitectureTests.Abstractions;
using NetArchTest.Rules;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.ArchitectureTests.Domain;

public sealed class DomainTests : BaseTest
{
    [Fact]
    public void DomainEvents_Should_BeSealed()
    {
        Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .Should()
            .BeSealed()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void DomainEvent_ShouldHave_NameEndingWith_DomainEvent()
    {
        Types.InAssembly(DomainAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEvent))
            .Should()
            .HaveNameEndingWith("DomainEvent")
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void ErrorCatalogs_Should_BeStatic()
    {
        Types.InAssembly(DomainAssembly)
            .That()
            .HaveNameEndingWith("Errors")
            .Should()
            .BeStatic()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void Entities_ShouldHave_PrivateParameterlessConstructor()
    {
        IEnumerable<Type> entityTypes = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(Entity))
            .GetTypes();

        Type[] entitiesWithoutPrivateParameterlessConstructor =
            [.. entityTypes.Where(LacksPrivateParameterlessConstructor)];

        entitiesWithoutPrivateParameterlessConstructor.ShouldBeEmpty();
    }

    [Fact]
    public void Entities_ShouldOnlyHave_PrivateConstructors()
    {
        IEnumerable<Type> entityTypes = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(Entity))
            .GetTypes();

        Type[] entitiesWithPublicConstructors = [.. entityTypes.Where(HasPublicConstructor)];

        entitiesWithPublicConstructors.ShouldBeEmpty();
    }

    private static bool LacksPrivateParameterlessConstructor(Type entityType)
    {
        ConstructorInfo[] nonPublicConstructors =
            entityType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);

        bool hasPrivateParameterlessConstructor = nonPublicConstructors.Any(constructor =>
            constructor.IsPrivate && constructor.GetParameters().Length == 0);

        return !hasPrivateParameterlessConstructor;
    }

    private static bool HasPublicConstructor(Type entityType)
    {
        ConstructorInfo[] publicConstructors = entityType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        return publicConstructors.Length > 0;
    }
}
