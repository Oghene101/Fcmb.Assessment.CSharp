using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Application.Outbox;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;
using Fcmb.Assessment.CSharp.Modules.Users.IntegrationEvents;
using Fcmb.Assessment.CSharp.Modules.Users.IntegrationTests.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Fcmb.Assessment.CSharp.Modules.Users.Application.Contracts.Dtos.Users;
using DomainUserType = Fcmb.Assessment.CSharp.Modules.Users.Domain.Users.UserType;
using IntegrationUserType = Fcmb.Assessment.CSharp.Modules.Users.IntegrationEvents.UserType;

namespace Fcmb.Assessment.CSharp.Modules.Users.IntegrationTests.Users;

public sealed class UserSignedUpDomainEventHandlerTests(
    IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string IntegrationEventsLane = "IntegrationEvents";

    [Theory]
    [InlineData(DomainUserType.Individual, IntegrationUserType.Individual)]
    [InlineData(DomainUserType.Corporate, IntegrationUserType.Corporate)]
    public async Task Should_PublishUserSignedUpIntegrationEvent_WhenAUserSignsUp(
        DomainUserType userType,
        IntegrationUserType expectedUserType)
    {
        SignUpRequest request = CommandHelpers.NewSignUpRequest(userType);

        Guid userId = await HttpClient.SignUpAsync(request, CancellationToken);

        UserSignedUpIntegrationEvent? published =
            await WaitForPublishedAsync<UserSignedUpIntegrationEvent>(signedUp => signedUp.UserId == userId);

        published.ShouldNotBeNull();
        published.FirstName.ShouldBe(request.FirstName);
        published.LastName.ShouldBe(request.LastName);
        published.Email.ShouldBe(request.Email);
        published.PhoneNumber.ShouldBe(request.PhoneNumber);
        published.Dob.ShouldBe(request.Dob);
        published.UserType.ShouldBe(expectedUserType);
    }

    [Fact]
    public async Task Should_PublishFromTheIntegrationEventsLane_AndRecordTheOutboxMessageAsProcessed()
    {
        SignUpRequest request = CommandHelpers.NewSignUpRequest();

        Guid userId = await HttpClient.SignUpAsync(request, CancellationToken);

        UserSignedUpIntegrationEvent? published =
            await WaitForPublishedAsync<UserSignedUpIntegrationEvent>(signedUp => signedUp.UserId == userId);

        published.ShouldNotBeNull();

        OutboxMessage? processedOutboxMessage = await Poller.WaitAsync(
            DeliveryTimeout,
            () => DbContext.Set<OutboxMessage>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    outboxMessage => outboxMessage.Id == published.Id && outboxMessage.ProcessedOn != null,
                    CancellationToken),
            CancellationToken);

        bool handlerRecorded = await DbContext.Set<OutboxMessageConsumer>()
            .AnyAsync(
                consumer => consumer.OutboxMessageId == published.Id
                            && consumer.Name == "UserSignedUpDomainEventHandler",
                CancellationToken);

        processedOutboxMessage.ShouldNotBeNull();
        processedOutboxMessage.Lane.ShouldBe(IntegrationEventsLane);
        processedOutboxMessage.Type.ShouldBe(nameof(UserSignedUpDomainEvent));
        handlerRecorded.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_PublishOnlyOnce_WhenTheSameDomainEventIsHandledAgain()
    {
        UserSignedUpDomainEvent domainEvent = NewUserSignedUpDomainEvent(DomainUserType.Individual);

        Result firstHandling = await HandleInItsOwnScopeAsync(domainEvent);
        Result secondHandling = await HandleInItsOwnScopeAsync(domainEvent);

        UserSignedUpIntegrationEvent? published =
            await WaitForPublishedAsync<UserSignedUpIntegrationEvent>(signedUp => signedUp.Id == domainEvent.Id);

        UserSignedUpIntegrationEvent[] publications =
            PublishedSoFar<UserSignedUpIntegrationEvent>(signedUp => signedUp.Id == domainEvent.Id);

        firstHandling.IsSuccess.ShouldBeTrue();
        secondHandling.IsSuccess.ShouldBeTrue();
        published.ShouldNotBeNull();
        publications.Length.ShouldBe(1);
    }

    [Fact]
    public async Task Should_ReturnFailure_AndPublishNothing_WhenTheUserTypeHasNoIntegrationCounterpart()
    {
        const DomainUserType unsupportedUserType = (DomainUserType)99;
        UserSignedUpDomainEvent domainEvent = NewUserSignedUpDomainEvent(unsupportedUserType);
        Error expectedError = UserErrors.UnsupportedUserType(unsupportedUserType);

        Result handling = await HandleInItsOwnScopeAsync(domainEvent);

        UserSignedUpIntegrationEvent[] publications =
            PublishedSoFar<UserSignedUpIntegrationEvent>(signedUp => signedUp.Id == domainEvent.Id);

        handling.IsSuccess.ShouldBeFalse();
        handling.Error.ShouldBe(expectedError);
        publications.ShouldBeEmpty();
    }

    private async Task<Result> HandleInItsOwnScopeAsync(UserSignedUpDomainEvent domainEvent)
    {
        await using AsyncServiceScope scope = ServiceProvider.CreateAsyncScope();

        IDomainEventHandler<UserSignedUpDomainEvent> handler =
            scope.ServiceProvider.GetRequiredService<IDomainEventHandler<UserSignedUpDomainEvent>>();

        return await handler.Handle(domainEvent, CancellationToken);
    }

    private static UserSignedUpDomainEvent NewUserSignedUpDomainEvent(DomainUserType userType)
    {
        SignUpRequest request = CommandHelpers.NewSignUpRequest();
        var domainEventId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();

        return new UserSignedUpDomainEvent(
            domainEventId,
            DateTimeOffset.UtcNow,
            userId,
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.Dob,
            userType);
    }
}
