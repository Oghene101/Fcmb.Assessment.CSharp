using Fcmb.Assessment.CSharp.Common.Application.Outbox;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Common.Infrastructure.UnitTests.Abstractions;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.UnitTests.Messaging;

public sealed class OutboxMessageProcessorTests
{
    private static readonly Error BrokerDown = Error.Dependency("Broker.Unreachable", "The broker did not answer.");
    private static readonly Error NoCounterpart = Error.Problem("User.Unsupported", "No counterpart.");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AHandledMessage_IsMarkedProcessed_AndKeepsNoneOfItsContent()
    {
        var handler = new RecordingHandler();
        await using var harness = MessagingHarness.Build(handler);
        OutboxMessage written = await harness.WriteOutboxMessageAsync();

        await harness.OutboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        OutboxMessage? storedOutboxMessage = await harness.StoredOutboxMessageAsync(written.Id);
        storedOutboxMessage.ShouldNotBeNull();
        storedOutboxMessage.ProcessedOn.ShouldBe(MessagingHarness.Now);
        storedOutboxMessage.Content.ShouldBe("{}");
        storedOutboxMessage.ClaimedUntil.ShouldBeNull();
        handler.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task ADependencyFailure_IsScheduledForRetry_WithExponentialBackoff()
    {
        await using var harness = MessagingHarness.Build(new RecordingHandler(BrokerDown));
        OutboxMessage written = await harness.WriteOutboxMessageAsync(retryCount: 1);

        await harness.OutboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        OutboxMessage? storedOutboxMessage = await harness.StoredOutboxMessageAsync(written.Id);
        storedOutboxMessage.ShouldNotBeNull();
        storedOutboxMessage.ProcessedOn.ShouldBeNull();
        storedOutboxMessage.RetryCount.ShouldBe(2);
        storedOutboxMessage.NextRetryOn.ShouldBe(MessagingHarness.Now.AddSeconds(60));
        storedOutboxMessage.ClaimedUntil.ShouldBeNull();
        storedOutboxMessage.Error.ShouldBe(BrokerDown.Message);
    }

    [Fact]
    public async Task AThrowingHandler_IsRetried()
    {
        var handler = new RecordingHandler(thrown: new InvalidOperationException("boom"));
        await using var harness = MessagingHarness.Build(handler);
        OutboxMessage written = await harness.WriteOutboxMessageAsync();

        await harness.OutboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        OutboxMessage? storedOutboxMessage = await harness.StoredOutboxMessageAsync(written.Id);
        storedOutboxMessage.ShouldNotBeNull();
        storedOutboxMessage.RetryCount.ShouldBe(1);
        storedOutboxMessage.NextRetryOn.ShouldBe(MessagingHarness.Now.AddSeconds(30));
        storedOutboxMessage.Error.ShouldNotBeNull().ShouldContain("boom");
    }

    [Fact]
    public async Task AFinalFailure_IsDeadLetteredAtOnce()
    {
        await using var harness = MessagingHarness.Build(new RecordingHandler(NoCounterpart));
        OutboxMessage written = await harness.WriteOutboxMessageAsync();

        await harness.OutboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        OutboxMessage? storedOutboxMessage = await harness.StoredOutboxMessageAsync(written.Id);
        storedOutboxMessage.ShouldBeNull();
        DeadLetteredOutboxMessage? deadLetteredOutboxMessage = await harness.DeadLetteredOutboxMessageAsync(written.Id);
        deadLetteredOutboxMessage.ShouldNotBeNull();
        deadLetteredOutboxMessage.RetryCount.ShouldBe(1);
        deadLetteredOutboxMessage.Content.ShouldBe(written.Content);
        deadLetteredOutboxMessage.Error.ShouldBe(NoCounterpart.Message);
    }

    [Fact]
    public async Task ADependencyFailure_OnTheLastRetry_IsDeadLettered()
    {
        int lastRetry = MessagingHarness.Lane.MaxRetries - 1;
        await using var harness = MessagingHarness.Build(new RecordingHandler(BrokerDown));
        OutboxMessage written = await harness.WriteOutboxMessageAsync(retryCount: lastRetry);

        await harness.OutboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        OutboxMessage? storedOutboxMessage = await harness.StoredOutboxMessageAsync(written.Id);
        storedOutboxMessage.ShouldBeNull();
        DeadLetteredOutboxMessage? deadLetteredOutboxMessage = await harness.DeadLetteredOutboxMessageAsync(written.Id);
        deadLetteredOutboxMessage.ShouldNotBeNull();
        deadLetteredOutboxMessage.RetryCount.ShouldBe(MessagingHarness.Lane.MaxRetries);
    }

    [Fact]
    public async Task UnreadableContent_IsDeadLetteredWithoutCallingAHandler()
    {
        var handler = new RecordingHandler();
        await using var harness = MessagingHarness.Build(handler);
        OutboxMessage written = await harness.WriteOutboxMessageAsync(content: "{ not json");

        await harness.OutboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        DeadLetteredOutboxMessage? deadLetteredOutboxMessage = await harness.DeadLetteredOutboxMessageAsync(written.Id);
        deadLetteredOutboxMessage.ShouldNotBeNull();
        deadLetteredOutboxMessage.Content.ShouldBe("{ not json");
        handler.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task AFailingHandler_StopsTheHandlersAfterIt()
    {
        var failingHandler = new RecordingHandler(BrokerDown);
        var handlerAfterTheFailure = new RecordingHandler();
        await using var harness = MessagingHarness.Build(failingHandler, handlerAfterTheFailure);
        OutboxMessage written = await harness.WriteOutboxMessageAsync();

        await harness.OutboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        failingHandler.Calls.ShouldBe(1);
        handlerAfterTheFailure.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task AnAlreadyProcessedMessage_IsLeftAlone()
    {
        var handler = new RecordingHandler();
        await using var harness = MessagingHarness.Build(handler);
        OutboxMessage written = await harness.WriteOutboxMessageAsync();
        await harness.OutboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        await harness.OutboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        handler.Calls.ShouldBe(1);
    }
}
