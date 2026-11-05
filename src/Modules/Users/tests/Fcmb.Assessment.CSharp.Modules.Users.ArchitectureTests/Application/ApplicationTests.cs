using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Modules.Users.ArchitectureTests.Abstractions;
using FluentValidation;
using NetArchTest.Rules;

namespace Fcmb.Assessment.CSharp.Modules.Users.ArchitectureTests.Application;

public sealed class ApplicationTests : BaseTest
{
    private static readonly NestedInUseCaseRule NestedInUseCase = new();

    [Fact]
    public void UseCase_Should_BeStatic()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .HaveNameEndingWith("UseCase")
            .Should()
            .BeStatic()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void Command_Should_BeSealed()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommand))
            .Or()
            .ImplementInterface(typeof(ICommand<>))
            .Should()
            .BeSealed()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void Command_ShouldHave_Name_Command()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommand))
            .Or()
            .ImplementInterface(typeof(ICommand<>))
            .Should()
            .HaveName("Command")
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void Command_Should_BeNestedInAUseCase()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommand))
            .Or()
            .ImplementInterface(typeof(ICommand<>))
            .Should()
            .MeetCustomRule(NestedInUseCase)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void CommandHandler_Should_NotBePublic()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Should()
            .NotBePublic()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void CommandHandler_Should_BeSealed()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Should()
            .BeSealed()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void CommandHandler_ShouldHave_Name_Handler()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Should()
            .HaveName("Handler")
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void CommandHandler_Should_BeNestedInAUseCase()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<>))
            .Or()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Should()
            .MeetCustomRule(NestedInUseCase)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void Query_Should_BeSealed()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQuery<>))
            .Should()
            .BeSealed()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void Query_ShouldHave_Name_Query()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQuery<>))
            .Should()
            .HaveName("Query")
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void QueryHandler_Should_NotBePublic()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .NotBePublic()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void QueryHandler_Should_BeSealed()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .BeSealed()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void QueryHandler_ShouldHave_Name_Handler()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IQueryHandler<,>))
            .Should()
            .HaveName("Handler")
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void Validator_Should_NotBePublic()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(AbstractValidator<>))
            .Should()
            .NotBePublic()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void Validator_Should_BeSealed()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(AbstractValidator<>))
            .Should()
            .BeSealed()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void Validator_ShouldHave_Name_Validator()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(AbstractValidator<>))
            .Should()
            .HaveName("Validator")
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void Validator_Should_BeNestedInAUseCase()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(AbstractValidator<>))
            .Should()
            .MeetCustomRule(NestedInUseCase)
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void DomainEventHandler_Should_NotBePublic()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEventHandler<>))
            .Should()
            .NotBePublic()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void DomainEventHandler_Should_BeSealed()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEventHandler<>))
            .Should()
            .BeSealed()
            .GetResult()
            .ShouldBeSuccessful();
    }

    [Fact]
    public void DomainEventHandler_ShouldHave_NameEndingWith_DomainEventHandler()
    {
        Types.InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(IDomainEventHandler<>))
            .Should()
            .HaveNameEndingWith("DomainEventHandler")
            .GetResult()
            .ShouldBeSuccessful();
    }
}
