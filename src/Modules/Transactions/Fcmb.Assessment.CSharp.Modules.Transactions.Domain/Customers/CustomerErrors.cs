using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Modules.Transactions.Domain.Customers;

public static class CustomerErrors
{
    public static Error UnsupportedCustomerType(string userType) =>
        Error.Problem("Customer.UnsupportedCustomerType", $"User type '{userType}' has no customer counterpart.");
}
