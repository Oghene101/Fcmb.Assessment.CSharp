using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;

// One per lane: claims only as many messages as it has free slots and refills a slot as soon as its message
// finishes. Every instance claims from the same table with READPAST, and a lapsed lease lets another take over.
public sealed class OutboxLaneProcessor<TUnitOfWork>(
    MessageLane lane,
    IServiceScopeFactory serviceScopeFactory,
    OutboxMessageProcessor<TUnitOfWork> messageProcessor,
    TimeProvider time,
    ILogger<OutboxLaneProcessor<TUnitOfWork>> logger) : BackgroundService
    where TUnitOfWork : ICommonUnitOfWork
{
    private const int ClaimFailureBackoffSeconds = 5;

    private readonly SemaphoreSlim _freeSlots = new(lane.Settings.Concurrency, lane.Settings.Concurrency);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        List<Task> messagesInFlight = [];

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                messagesInFlight.RemoveAll(message => message.IsCompleted);

                if (_freeSlots.CurrentCount == 0)
                {
                    await Task.WhenAny(messagesInFlight);

                    continue;
                }

                ClaimOutcome claim = await ClaimAsync(_freeSlots.CurrentCount, stoppingToken);

                foreach (Guid claimedId in claim.ClaimedIds)
                {
                    await _freeSlots.WaitAsync(stoppingToken);

                    Task messageInFlight = ProcessAndFreeSlotAsync(claimedId, stoppingToken);

                    messagesInFlight.Add(messageInFlight);
                }

                if (claim.ClaimedIds.Count == 0)
                {
                    await WaitAsync(claim.WaitBeforeNextClaim, stoppingToken);
                }
            }
        }
        finally
        {
            await Task.WhenAll(messagesInFlight);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _freeSlots.Dispose();
    }

    private sealed record ClaimOutcome(
        IReadOnlyList<Guid> ClaimedIds,
        TimeSpan WaitBeforeNextClaim);

    private async Task<ClaimOutcome> ClaimAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        var pollInterval = TimeSpan.FromMilliseconds(lane.Settings.PollIntervalMilliseconds);

        var claimFailureBackoff = TimeSpan.FromSeconds(ClaimFailureBackoffSeconds);

        try
        {
            await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
            TUnitOfWork uOw = scope.ServiceProvider.GetRequiredService<TUnitOfWork>();

            DateTimeOffset now = time.GetUtcNow();

            DateTimeOffset claimedUntil = now.AddSeconds(lane.LeaseSeconds);

            // An idle lane polls several times a second; tracing each empty poll would export nothing useful.
            using IDisposable quietClaim = SuppressInstrumentationScope.Begin();

            cancellationToken.ThrowIfCancellationRequested();

            Guid[] claimedIds = await uOw.OutboxMessagesReadRepository.ClaimAsync(lane.Name, limit, now, claimedUntil);

            return new ClaimOutcome(claimedIds, pollInterval);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Claiming {Module} {Lane} outbox messages failed; trying again in {Backoff}",
                lane.Module, lane.Name, claimFailureBackoff);

            return new ClaimOutcome([], claimFailureBackoff);
        }
    }

    private async Task ProcessAndFreeSlotAsync(
        Guid outboxMessageId,
        CancellationToken stoppingToken)
    {
        try
        {
            await messageProcessor.ProcessAsync(outboxMessageId, lane.Settings, stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Recording the outcome failed (the database is down, say); the lease lapses and it is claimed again.
            logger.LogError(exception, "{Module} outbox message {MessageId} in the {Lane} lane could not be processed",
                lane.Module, outboxMessageId, lane.Name);
        }
        finally
        {
            _freeSlots.Release();
        }
    }

    private async Task WaitAsync(
        TimeSpan wait,
        CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(wait, time, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Stopping: the loop condition ends the lane.
        }
    }
}
