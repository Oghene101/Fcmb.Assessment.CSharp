using Fcmb.Assessment.CSharp.Common.Application.Inbox;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Application.Outbox;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Inbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Outbox;
using Fcmb.Assessment.CSharp.Common.Infrastructure.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.UnitTests.Abstractions;

internal sealed class MessagingHarness(
    ServiceProvider provider) : IAsyncDisposable
{
    public static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    public static readonly MessageLaneSettings Lane = new()
    {
        Concurrency = 1,
        PollIntervalMilliseconds = 100,
        MaxRetries = 3,
        RetryDelaySeconds = 30
    };

    public OutboxMessageProcessor<TestUnitOfWork> OutboxProcessor =>
        provider.GetRequiredService<OutboxMessageProcessor<TestUnitOfWork>>();

    public InboxMessageProcessor<TestUnitOfWork> InboxProcessor =>
        provider.GetRequiredService<InboxMessageProcessor<TestUnitOfWork>>();

    public static MessagingHarness Build(params RecordingHandler[] handlers)
    {
        string databaseName = $"messaging-tests-{Guid.CreateVersion7()}";

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
        services.AddDbContext<MessagingTestDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<TestUnitOfWork>();
        services.AddSingleton(new MessagingModule<TestUnitOfWork>("Tests"));
        services.AddSingleton<OutboxMessageProcessor<TestUnitOfWork>>();
        services.AddSingleton<InboxMessageProcessor<TestUnitOfWork>>();

        foreach (RecordingHandler handler in handlers)
        {
            services.AddSingleton<IDomainEventHandler<CustomerNotifiedDomainEvent>>(handler);
            services.AddSingleton<IIntegrationEventHandler<CustomerRegisteredIntegrationEvent>>(handler);
        }

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        return new MessagingHarness(serviceProvider);
    }

    public async Task<OutboxMessage> WriteOutboxMessageAsync(
        string? content = null,
        int retryCount = 0)
    {
        var customerNotified = new CustomerNotifiedDomainEvent(Guid.CreateVersion7(), Now);

        string serializedEvent = JsonConvert.SerializeObject(customerNotified, SerializerSettings.Instance);

        var outboxMessage = new OutboxMessage
        {
            Id = customerNotified.Id,
            Type = nameof(CustomerNotifiedDomainEvent),
            Lane = MessageLanes.Default,
            Content = content ?? serializedEvent,
            OccurredOn = customerNotified.OccurredOn,
            ClaimedUntil = Now.AddMinutes(2),
            RetryCount = retryCount
        };

        await SaveAsync(outboxMessage);

        return outboxMessage;
    }

    public async Task<InboxMessage> WriteInboxMessageAsync(
        string? content = null,
        int retryCount = 0)
    {
        var customerRegistered = new CustomerRegisteredIntegrationEvent(Guid.CreateVersion7(), Now);

        string serializedEvent = JsonConvert.SerializeObject(customerRegistered, SerializerSettings.Instance);

        var inboxMessage = new InboxMessage
        {
            Id = customerRegistered.Id,
            Type = nameof(CustomerRegisteredIntegrationEvent),
            Lane = MessageLanes.Default,
            Content = content ?? serializedEvent,
            OccurredOn = customerRegistered.OccurredOn,
            ClaimedUntil = Now.AddMinutes(2),
            RetryCount = retryCount
        };

        await SaveAsync(inboxMessage);

        return inboxMessage;
    }

    public Task<OutboxMessage?> StoredOutboxMessageAsync(Guid id) =>
        QueryAsync(db => db.Set<OutboxMessage>().AsNoTracking().SingleOrDefaultAsync(message => message.Id == id));

    public Task<DeadLetteredOutboxMessage?> DeadLetteredOutboxMessageAsync(Guid id) =>
        QueryAsync(db => db.Set<DeadLetteredOutboxMessage>().AsNoTracking()
            .SingleOrDefaultAsync(message => message.Id == id));

    public Task<InboxMessage?> StoredInboxMessageAsync(Guid id) =>
        QueryAsync(db => db.Set<InboxMessage>().AsNoTracking().SingleOrDefaultAsync(message => message.Id == id));

    public Task<DeadLetteredInboxMessage?> DeadLetteredInboxMessageAsync(Guid id) =>
        QueryAsync(db => db.Set<DeadLetteredInboxMessage>().AsNoTracking()
            .SingleOrDefaultAsync(message => message.Id == id));

    public async Task<TResult> InScopeAsync<TResult>(Func<TestUnitOfWork, Task<TResult>> work)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        TestUnitOfWork uOw = scope.ServiceProvider.GetRequiredService<TestUnitOfWork>();

        TResult result = await work(uOw);

        return result;
    }

    public ValueTask DisposeAsync() => provider.DisposeAsync();

    private async Task SaveAsync(object message)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        MessagingTestDbContext db = scope.ServiceProvider.GetRequiredService<MessagingTestDbContext>();

        db.Add(message);

        await db.SaveChangesAsync();
    }

    private async Task<TResult> QueryAsync<TResult>(Func<MessagingTestDbContext, Task<TResult>> query)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        MessagingTestDbContext db = scope.ServiceProvider.GetRequiredService<MessagingTestDbContext>();

        TResult result = await query(db);

        return result;
    }
}
