using Fcmb.Assessment.CSharp.ArchitectureTests.Abstractions;
using NetArchTest.Rules;

namespace Fcmb.Assessment.CSharp.ArchitectureTests.Layers;

public sealed class CommonLayerTests : BaseTest
{
    [Fact]
    public void CommonDomain_ShouldNotHaveDependencyOn_AnyOuterLayer()
    {
        string[] outerLayers = [CommonApplicationNamespace, CommonPresentationNamespace, CommonInfrastructureNamespace];

        Types.InAssembly(CommonDomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(outerLayers)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void CommonApplication_ShouldNotHaveDependencyOn_PresentationOrInfrastructure()
    {
        string[] outerLayers = [CommonPresentationNamespace, CommonInfrastructureNamespace];

        Types.InAssembly(CommonApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(outerLayers)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void CommonPresentation_ShouldNotHaveDependencyOn_Infrastructure()
    {
        Types.InAssembly(CommonPresentationAssembly)
            .Should()
            .NotHaveDependencyOn(CommonInfrastructureNamespace)
            .GetResult()
            .ShouldBeSuccessful();
    }
}
