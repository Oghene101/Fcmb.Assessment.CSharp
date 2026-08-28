using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;

namespace Fcmb.Assessment.CSharp.Modules.Users.Application.Contracts.Dtos;

public static class Users
{
    public enum TransferType
    {
        IntraBank,
        InterBank,
    }

    public sealed record SignUpRequest(
        string FirstName,
        string LastName,
        string PhoneNumber,
        DateOnly Dob,
        UserType UserType,
        string Email,
        string Password);

    public sealed record SignUpResponse(
        Guid UserId);
}
