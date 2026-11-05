using Fcmb.Assessment.CSharp.Common.Application.Messaging;
using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Transactions.Application.Data;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Customers;
using Fcmb.Assessment.CSharp.Modules.Users.IntegrationEvents;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Presentation.Customers;

// Saved by the idempotent decorator together with the record that this event was handled.
internal sealed class UserSignedUpIntegrationEventHandler(
    IUnitOfWork uOw)
    : IIntegrationEventHandler<UserSignedUpIntegrationEvent>
{
    public async Task<Result> Handle(UserSignedUpIntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        CustomerType? customerType = integrationEvent.UserType switch
        {
            UserType.Individual => CustomerType.Individual,
            UserType.Corporate => CustomerType.Corporate,
            _ => null
        };

        if (customerType is null)
        {
            string unsupportedUserType = integrationEvent.UserType.ToString();

            return CustomerErrors.UnsupportedCustomerType(unsupportedUserType);
        }

        Customer customer = Customer.Create(
            integrationEvent.FirstName,
            integrationEvent.LastName,
            integrationEvent.Email,
            integrationEvent.PhoneNumber,
            integrationEvent.Dob,
            customerType.Value);

        await uOw.CustomersWriteRepository.AddAsync(customer, cancellationToken);

        return Result.Success();
    }
}
