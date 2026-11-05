using Fcmb.Assessment.CSharp.Common.Application.Inbox;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Common.Infrastructure.UnitTests.Abstractions;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.UnitTests.Messaging;

public sealed class InboxMessageProcessorTests
{
    private static readonly Error BrokerDown = Error.Dependency("Broker.Unreachable", "The broker did not answer.");
    private static readonly Error NoCounterpart = Error.Problem("User.Unsupported", "No counterpart.");

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AHandledMessage_IsMarkedProcessed_AndKeepsNoneOfItsContent()
    {
        var handler = new RecordingHandler();
        await using var harness = MessagingHarness.Build(handler);
        InboxMessage written = await harness.WriteInboxMessageAsync();

        await harness.InboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        InboxMessage? storedInboxMessage = await harness.StoredInboxMessageAsync(written.Id);
        storedInboxMessage.ShouldNotBeNull();
        storedInboxMessage.ProcessedOn.ShouldBe(MessagingHarness.Now);
        storedInboxMessage.Content.ShouldBe("{}");
        storedInboxMessage.ClaimedUntil.ShouldBeNull();
        handler.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task ADependencyFailure_IsScheduledForRetry_WithExponentialBackoff()
    {
        await using var harness = MessagingHarness.Build(new RecordingHandler(BrokerDown));
        InboxMessage written = await harness.WriteInboxMessageAsync(retryCount: 1);

        await harness.InboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        InboxMessage? storedInboxMessage = await harness.StoredInboxMessageAsync(written.Id);
        storedInboxMessage.ShouldNotBeNull();
        storedInboxMessage.ProcessedOn.ShouldBeNull();
        storedInboxMessage.RetryCount.ShouldBe(2);
        storedInboxMessage.NextRetryOn.ShouldBe(MessagingHarness.Now.AddSeconds(60));
        storedInboxMessage.ClaimedUntil.ShouldBeNull();
        storedInboxMessage.Error.ShouldBe(BrokerDown.Message);
    }

    [Fact]
    public async Task AThrowingHandler_IsRetried()
    {
        var handler = new RecordingHandler(thrown: new InvalidOperationException("boom"));
        await using var harness = MessagingHarness.Build(handler);
        InboxMessage written = await harness.WriteInboxMessageAsync();

        await harness.InboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        InboxMessage? storedInboxMessage = await harness.StoredInboxMessageAsync(written.Id);
        storedInboxMessage.ShouldNotBeNull();
        storedInboxMessage.RetryCount.ShouldBe(1);
        storedInboxMessage.NextRetryOn.ShouldBe(MessagingHarness.Now.AddSeconds(30));
        storedInboxMessage.Error.ShouldNotBeNull().ShouldContain("boom");
    }

    [Fact]
    public async Task AFinalFailure_IsDeadLetteredAtOnce()
    {
        await using var harness = MessagingHarness.Build(new RecordingHandler(NoCounterpart));
        InboxMessage written = await harness.WriteInboxMessageAsync();

        await harness.InboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        InboxMessage? storedInboxMessage = await harness.StoredInboxMessageAsync(written.Id);
        storedInboxMessage.ShouldBeNull();
        DeadLetteredInboxMessage? deadLetteredInboxMessage = await harness.DeadLetteredInboxMessageAsync(written.Id);
        deadLetteredInboxMessage.ShouldNotBeNull();
        deadLetteredInboxMessage.RetryCount.ShouldBe(1);
        deadLetteredInboxMessage.Content.ShouldBe(written.Content);
        deadLetteredInboxMessage.Error.ShouldBe(NoCounterpart.Message);
    }

    [Fact]
    public async Task ADependencyFailure_OnTheLastRetry_IsDeadLettered()
    {
        int lastRetry = MessagingHarness.Lane.MaxRetries - 1;
        await using var harness = MessagingHarness.Build(new RecordingHandler(BrokerDown));
        InboxMessage written = await harness.WriteInboxMessageAsync(retryCount: lastRetry);

        await harness.InboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        InboxMessage? storedInboxMessage = await harness.StoredInboxMessageAsync(written.Id);
        storedInboxMessage.ShouldBeNull();
        DeadLetteredInboxMessage? deadLetteredInboxMessage = await harness.DeadLetteredInboxMessageAsync(written.Id);
        deadLetteredInboxMessage.ShouldNotBeNull();
        deadLetteredInboxMessage.RetryCount.ShouldBe(MessagingHarness.Lane.MaxRetries);
    }

    [Fact]
    public async Task UnreadableContent_IsDeadLetteredWithoutCallingAHandler()
    {
        var handler = new RecordingHandler();
        await using var harness = MessagingHarness.Build(handler);
        InboxMessage written = await harness.WriteInboxMessageAsync(content: "{ not json");

        await harness.InboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        DeadLetteredInboxMessage? deadLetteredInboxMessage = await harness.DeadLetteredInboxMessageAsync(written.Id);
        deadLetteredInboxMessage.ShouldNotBeNull();
        deadLetteredInboxMessage.Content.ShouldBe("{ not json");
        handler.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task AFailingHandler_StopsTheHandlersAfterIt()
    {
        var failingHandler = new RecordingHandler(BrokerDown);
        var handlerAfterTheFailure = new RecordingHandler();
        await using var harness = MessagingHarness.Build(failingHandler, handlerAfterTheFailure);
        InboxMessage written = await harness.WriteInboxMessageAsync();

        await harness.InboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        failingHandler.Calls.ShouldBe(1);
        handlerAfterTheFailure.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task AnAlreadyProcessedMessage_IsLeftAlone()
    {
        var handler = new RecordingHandler();
        await using var harness = MessagingHarness.Build(handler);
        InboxMessage written = await harness.WriteInboxMessageAsync();
        await harness.InboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        await harness.InboxProcessor.ProcessAsync(written.Id, MessagingHarness.Lane, Token);

        handler.Calls.ShouldBe(1);
    }
}
