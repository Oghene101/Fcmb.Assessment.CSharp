using Fcmb.Assessment.CSharp.Common.Application.Inbox;
using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Customers;
using Fcmb.Assessment.CSharp.Modules.Transactions.IntegrationTests.Abstractions;
using Fcmb.Assessment.CSharp.Modules.Users.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.IntegrationTests.Customers;

public sealed class UserSignedUpIntegrationEventHandlerTests(
    IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Theory]
    [InlineData(UserType.Individual, CustomerType.Individual)]
    [InlineData(UserType.Corporate, CustomerType.Corporate)]
    public async Task Should_CreateTheCustomer_WhenAUserSignedUpEventIsHandled(
        UserType userType,
        CustomerType expectedCustomerType)
    {
        UserSignedUpIntegrationEvent signedUp = CommandHelpers.NewUserSignedUpIntegrationEvent(userType);

        Result handling = await HandleInItsOwnScopeAsync(signedUp);

        Customer? customer = await FindCustomerAsync(signedUp.Email);

        handling.IsSuccess.ShouldBeTrue();
        customer.ShouldNotBeNull();
        customer.FirstName.ShouldBe(signedUp.FirstName);
        customer.LastName.ShouldBe(signedUp.LastName);
        customer.PhoneNumber.ShouldBe(signedUp.PhoneNumber);
        customer.Dob.ShouldBe(signedUp.Dob);
        customer.CustomerType.ShouldBe(expectedCustomerType);
    }

    [Fact]
    public async Task Should_CreateTheCustomerOnce_WhenTheSameEventIsHandledAgain()
    {
        UserSignedUpIntegrationEvent signedUp = CommandHelpers.NewUserSignedUpIntegrationEvent(UserType.Individual);

        Result firstHandling = await HandleInItsOwnScopeAsync(signedUp);
        Result secondHandling = await HandleInItsOwnScopeAsync(signedUp);

        int customers = await DbContext.Customers
            .CountAsync(customer => customer.Email == signedUp.Email, CancellationToken);

        firstHandling.IsSuccess.ShouldBeTrue();
        secondHandling.IsSuccess.ShouldBeTrue();
        customers.ShouldBe(1);
    }

    [Fact]
    public async Task Should_ReturnFailure_AndCreateNoCustomer_WhenTheUserTypeHasNoCustomerCounterpart()
    {
        const UserType unsupportedUserType = (UserType)99;
        UserSignedUpIntegrationEvent signedUp = CommandHelpers.NewUserSignedUpIntegrationEvent(unsupportedUserType);
        string unsupportedUserTypeName = unsupportedUserType.ToString();
        Error expectedError = CustomerErrors.UnsupportedCustomerType(unsupportedUserTypeName);

        Result handling = await HandleInItsOwnScopeAsync(signedUp);

        Customer? customer = await FindCustomerAsync(signedUp.Email);

        handling.IsSuccess.ShouldBeFalse();
        handling.Error.ShouldBe(expectedError);
        customer.ShouldBeNull();
    }

    [Fact]
    public async Task Should_CreateTheCustomer_WhenTheEventArrivesThroughTheBus()
    {
        UserSignedUpIntegrationEvent signedUp = CommandHelpers.NewUserSignedUpIntegrationEvent(UserType.Corporate);

        await Harness.Bus.Publish(signedUp, CancellationToken);

        Customer? customer = await Poller.WaitAsync(
            DeliveryTimeout,
            () => FindCustomerAsync(signedUp.Email),
            CancellationToken);

        InboxMessage? inboxMessage = await Poller.WaitAsync(
            DeliveryTimeout,
            () => FindProcessedInboxMessageAsync(signedUp.Id),
            CancellationToken);

        customer.ShouldNotBeNull();
        customer.CustomerType.ShouldBe(CustomerType.Corporate);
        inboxMessage.ShouldNotBeNull();
        inboxMessage.Type.ShouldBe(nameof(UserSignedUpIntegrationEvent));
    }

    [Fact]
    public async Task Should_StoreARedeliveredEventOnce_AndCreateOneCustomer()
    {
        UserSignedUpIntegrationEvent signedUp = CommandHelpers.NewUserSignedUpIntegrationEvent(UserType.Individual);

        await Harness.Bus.Publish(signedUp, CancellationToken);
        await Harness.Bus.Publish(signedUp, CancellationToken);

        UserSignedUpIntegrationEvent[]? bothDeliveries = await Poller.WaitAsync(
            DeliveryTimeout,
            () => BothDeliveriesConsumedAsync(signedUp.Id),
            CancellationToken);

        InboxMessage? inboxMessage = await Poller.WaitAsync(
            DeliveryTimeout,
            () => FindProcessedInboxMessageAsync(signedUp.Id),
            CancellationToken);

        int inboxMessages = await DbContext.Set<InboxMessage>()
            .CountAsync(message => message.Id == signedUp.Id, CancellationToken);

        int customers = await DbContext.Customers
            .CountAsync(customer => customer.Email == signedUp.Email, CancellationToken);

        bothDeliveries.ShouldNotBeNull();
        inboxMessage.ShouldNotBeNull();
        inboxMessages.ShouldBe(1);
        customers.ShouldBe(1);
    }

    private async Task<Result> HandleInItsOwnScopeAsync(UserSignedUpIntegrationEvent signedUp)
    {
        await using AsyncServiceScope scope = ServiceProvider.CreateAsyncScope();

        IIntegrationEventHandler<UserSignedUpIntegrationEvent> handler =
            scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<UserSignedUpIntegrationEvent>>();

        return await handler.Handle(signedUp, CancellationToken);
    }

    private Task<Customer?> FindCustomerAsync(string email) =>
        DbContext.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(customer => customer.Email == email, CancellationToken);

    private Task<InboxMessage?> FindProcessedInboxMessageAsync(Guid eventId) =>
        DbContext.Set<InboxMessage>()
            .AsNoTracking()
            .SingleOrDefaultAsync(message => message.Id == eventId && message.ProcessedOn != null, CancellationToken);

    private Task<UserSignedUpIntegrationEvent[]?> BothDeliveriesConsumedAsync(Guid eventId)
    {
        UserSignedUpIntegrationEvent[] deliveries =
            ConsumedSoFar<UserSignedUpIntegrationEvent>(delivered => delivered.Id == eventId);

        UserSignedUpIntegrationEvent[]? bothDeliveries = deliveries.Length == 2 ? deliveries : null;

        return Task.FromResult(bothDeliveries);
    }
}
