using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Emails;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.PhoneNumbers;

namespace Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;

public sealed class User : Entity
{
    private User()
    {
    }

    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public DateOnly Dob { get; private set; }
    public UserType UserType { get; private set; }
    public string IdentityId { get; private set; }

    //Navigation props
    public ICollection<Email> Emails { get; private set; } = [];
    public ICollection<PhoneNumber> PhoneNumbers { get; private set; } = [];

    public static Result<User> Create(
        string firstName,
        string lastName,
        DateOnly dob,
        UserType userType,
        string identityId)
    {
        var id = Guid.CreateVersion7();
        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Dob = dob,
            UserType = userType,
            IdentityId = identityId
        };

        user.InitializeAudit(id, id.ToString());

        return user;
    }
}
