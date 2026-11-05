using Fcmb.Assessment.CSharp.IntegrationTests.Abstractions;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Customers;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;
using Microsoft.EntityFrameworkCore;
using SignUpRequest = Fcmb.Assessment.CSharp.Modules.Users.Application.Contracts.Dtos.Users.SignUpRequest;

namespace Fcmb.Assessment.CSharp.IntegrationTests.SignUp;

public sealed class SignUpTests(
    IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Theory]
    [InlineData(UserType.Individual, CustomerType.Individual)]
    [InlineData(UserType.Corporate, CustomerType.Corporate)]
    public async Task SignUp_Should_PropagateToTheTransactionsModule_AsACustomer(
        UserType userType,
        CustomerType expectedCustomerType)
    {
        SignUpRequest request = CommandHelpers.NewSignUpRequest(userType);

        await HttpClient.SignUpAsync(request, CancellationToken);

        Customer? customer = await Poller.WaitAsync(
            DeliveryTimeout,
            () => TransactionsDbContext.Customers
                .AsNoTracking()
                .SingleOrDefaultAsync(storedCustomer => storedCustomer.Email == request.Email, CancellationToken),
            CancellationToken);

        customer.ShouldNotBeNull();
        customer.FirstName.ShouldBe(request.FirstName);
        customer.LastName.ShouldBe(request.LastName);
        customer.PhoneNumber.ShouldBe(request.PhoneNumber);
        customer.Dob.ShouldBe(request.Dob);
        customer.CustomerType.ShouldBe(expectedCustomerType);
    }

    [Fact]
    public async Task SignUp_Should_CreateOneCustomer_PerSignedUpUser()
    {
        SignUpRequest firstRequest = CommandHelpers.NewSignUpRequest(UserType.Individual);
        SignUpRequest secondRequest = CommandHelpers.NewSignUpRequest(UserType.Individual);

        await HttpClient.SignUpAsync(firstRequest, CancellationToken);
        await HttpClient.SignUpAsync(secondRequest, CancellationToken);

        Customer? secondCustomer = await Poller.WaitAsync(
            DeliveryTimeout,
            () => TransactionsDbContext.Customers
                .AsNoTracking()
                .SingleOrDefaultAsync(storedCustomer => storedCustomer.Email == secondRequest.Email, CancellationToken),
            CancellationToken);

        int firstUsersCustomers = await TransactionsDbContext.Customers
            .CountAsync(storedCustomer => storedCustomer.Email == firstRequest.Email, CancellationToken);

        secondCustomer.ShouldNotBeNull();
        firstUsersCustomers.ShouldBe(1);
    }
}
