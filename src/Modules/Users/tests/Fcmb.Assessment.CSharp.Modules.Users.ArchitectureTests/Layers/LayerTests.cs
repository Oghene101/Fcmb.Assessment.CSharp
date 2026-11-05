using Fcmb.Assessment.CSharp.Modules.Users.ArchitectureTests.Abstractions;
using NetArchTest.Rules;

namespace Fcmb.Assessment.CSharp.Modules.Users.ArchitectureTests.Layers;

public sealed class LayerTests : BaseTest
{
    [Fact]
    public void DomainLayer_ShouldNotHaveDependencyOn_ApplicationLayer()
    {
        Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(ApplicationLayer)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void DomainLayer_ShouldNotHaveDependencyOn_PresentationLayer()
    {
        Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(PresentationLayer)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void DomainLayer_ShouldNotHaveDependencyOn_InfrastructureLayer()
    {
        Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureLayer)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void ApplicationLayer_ShouldNotHaveDependencyOn_PresentationLayer()
    {
        Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(PresentationLayer)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void ApplicationLayer_ShouldNotHaveDependencyOn_InfrastructureLayer()
    {
        Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureLayer)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void PresentationLayer_ShouldNotHaveDependencyOn_InfrastructureLayer()
    {
        Types.InAssembly(PresentationAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureLayer)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void IntegrationEvents_ShouldNotHaveDependencyOn_AnyOtherLayer()
    {
        string[] moduleLayers = [DomainLayer, ApplicationLayer, PresentationLayer, InfrastructureLayer];

        Types.InAssembly(IntegrationEventsAssembly)
            .Should()
            .NotHaveDependencyOnAny(moduleLayers)
            .GetResult()
            .ShouldBeSuccessful();
    }
}
