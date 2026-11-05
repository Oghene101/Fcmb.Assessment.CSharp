using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Inbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure.UnitTests.Abstractions;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.UnitTests.Messaging;

public sealed class IdempotentHandlerTests
{
    private static readonly Error BrokerDown = Error.Dependency("Broker.Unreachable", "The broker did not answer.");

    private static readonly CustomerNotifiedDomainEvent CustomerNotified =
        new(Guid.CreateVersion7(), MessagingHarness.Now);

    private static readonly CustomerRegisteredIntegrationEvent CustomerRegistered =
        new(Guid.CreateVersion7(), MessagingHarness.Now);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ADomainEvent_IsHandledOnce_HoweverOftenItIsDelivered()
    {
        var handler = new RecordingHandler();
        await using var harness = MessagingHarness.Build();

        Result firstDelivery = await HandleDomainEventAsync(harness, handler);
        Result secondDelivery = await HandleDomainEventAsync(harness, handler);

        firstDelivery.IsSuccess.ShouldBeTrue();
        secondDelivery.IsSuccess.ShouldBeTrue();
        handler.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task AFailedDomainEvent_IsNotRecordedAsHandled_SoTheRetryRunsItAgain()
    {
        var failingHandler = new RecordingHandler(BrokerDown);
        await using var harness = MessagingHarness.Build();

        Result firstDelivery = await HandleDomainEventAsync(harness, failingHandler);
        Result secondDelivery = await HandleDomainEventAsync(harness, failingHandler);

        firstDelivery.Error.ShouldBe(BrokerDown);
        secondDelivery.Error.ShouldBe(BrokerDown);
        failingHandler.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task AnIntegrationEvent_IsHandledOnce_HoweverOftenItIsDelivered()
    {
        var handler = new RecordingHandler();
        await using var harness = MessagingHarness.Build();

        Result firstDelivery = await HandleIntegrationEventAsync(harness, handler);
        Result secondDelivery = await HandleIntegrationEventAsync(harness, handler);

        firstDelivery.IsSuccess.ShouldBeTrue();
        secondDelivery.IsSuccess.ShouldBeTrue();
        handler.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task AFailedIntegrationEvent_IsNotRecordedAsHandled_SoTheRetryRunsItAgain()
    {
        var failingHandler = new RecordingHandler(BrokerDown);
        await using var harness = MessagingHarness.Build();

        Result firstDelivery = await HandleIntegrationEventAsync(harness, failingHandler);
        Result secondDelivery = await HandleIntegrationEventAsync(harness, failingHandler);

        firstDelivery.Error.ShouldBe(BrokerDown);
        secondDelivery.Error.ShouldBe(BrokerDown);
        failingHandler.Calls.ShouldBe(2);
    }

    private static Task<Result> HandleDomainEventAsync(
        MessagingHarness harness,
        RecordingHandler handler) =>
        harness.InScopeAsync(uOw =>
            new IdempotentDomainEventHandler<CustomerNotifiedDomainEvent, TestUnitOfWork>(handler, uOw)
                .Handle(CustomerNotified, Token));

    private static Task<Result> HandleIntegrationEventAsync(
        MessagingHarness harness,
        RecordingHandler handler) =>
        harness.InScopeAsync(uOw =>
            new IdempotentIntegrationEventHandler<CustomerRegisteredIntegrationEvent, TestUnitOfWork>(handler, uOw)
                .Handle(CustomerRegistered, Token));
}
