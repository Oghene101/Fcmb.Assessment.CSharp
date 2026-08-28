using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Modules.Users.Domain.PhoneNumbers;

public sealed class PhoneNumber : Entity
{
    private PhoneNumber()
    {
    }

    public string Number { get; private set; }
    public Guid UserId { get; private set; }

    public static Result<PhoneNumber> Create(
        string number,
        Guid userId)
    {
        var phoneNumber = new PhoneNumber
        {
            Number = number,
            UserId = userId
        };

        phoneNumber.InitializeAudit(
            Guid.CreateVersion7(),
            userId.ToString());

        return phoneNumber;
    }
}
