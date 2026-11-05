using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Customers;
using Fcmb.Assessment.CSharp.Modules.Transactions.UnitTests.Abstractions;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.UnitTests.Customers;

public sealed class CustomerTests : BaseTest
{
    private static readonly DateOnly Dob = new(1988, 2, 29);

    [Theory]
    [InlineData(CustomerType.Individual)]
    [InlineData(CustomerType.Corporate)]
    public void Create_ShouldReturnCustomer_WithTheGivenDetails(CustomerType customerType)
    {
        string firstName = Faker.Name.FirstName();
        string lastName = Faker.Name.LastName();
        string email = Faker.Internet.Email();
        string phoneNumber = Faker.Phone.PhoneNumber("+234##########");

        Result<Customer> result = Customer.Create(
            firstName,
            lastName,
            email,
            phoneNumber,
            Dob,
            customerType);

        result.IsSuccess.ShouldBeTrue();

        Customer customer = result.Value;

        customer.FirstName.ShouldBe(firstName);
        customer.LastName.ShouldBe(lastName);
        customer.Email.ShouldBe(email);
        customer.PhoneNumber.ShouldBe(phoneNumber);
        customer.Dob.ShouldBe(Dob);
        customer.CustomerType.ShouldBe(customerType);
    }

    [Fact]
    public void Create_ShouldStampTheAudit_AsCreatedByTheCustomerItself()
    {
        Customer customer = CreateCustomer();

        string customerId = customer.Id.ToString();

        customer.Id.ShouldNotBe(Guid.Empty);
        customer.CreatedBy.ShouldBe(customerId);
        customer.LastUpdatedBy.ShouldBe(customerId);
        customer.DeletedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldStartTheCustomer_WithNoAccounts()
    {
        Customer customer = CreateCustomer();

        customer.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public void Create_ShouldNotRaise_AnyDomainEvent()
    {
        Customer customer = CreateCustomer();

        customer.DomainEvents.ShouldBeEmpty();
    }

    private static Customer CreateCustomer()
    {
        string firstName = Faker.Name.FirstName();
        string lastName = Faker.Name.LastName();
        string email = Faker.Internet.Email();
        string phoneNumber = Faker.Phone.PhoneNumber("+234##########");

        return Customer.Create(
            firstName,
            lastName,
            email,
            phoneNumber,
            Dob,
            CustomerType.Individual);
    }
}
