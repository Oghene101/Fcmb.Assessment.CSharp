using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Accounts;
using Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Rewards;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Customers;

public sealed class Customer : Entity
{
    private Customer()
    {
    }

    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string PhoneNumber { get; private set; }
    public DateOnly Dob { get; private set; }
    public CustomerType CustomerType { get; private set; }

    //Navigation props
    public ICollection<Account> Accounts { get; private set; } = [];
    public Reward Reward { get; private set; } = null!;

    public static Result<Customer> Create(
        string firstName,
        string lastName,
        string email,
        string phoneNumber,
        DateOnly dob,
        CustomerType customerType)
    {
        var id = Guid.CreateVersion7();
        var customer = new Customer
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            Dob = dob,
            CustomerType = customerType
        };
        customer.InitializeAudit(id, id.ToString());

        return customer;
    }
}
