using Fcmb.Assessment.CSharp.Common.Domain;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;

namespace Fcmb.Assessment.CSharp.Modules.Users.Domain.Emails;

public sealed class Email : Entity
{
    private Email()
    {
    }

    public string Address { get; private set; }
    public Guid UserId { get; private set; }

    //Navigation props
    public User User { get; private set; } = null!;

    public static Result<Email> Create(string address, Guid userId)
    {
        var email = new Email
        {
            Address = address,
            UserId = userId,
        };

        email.InitializeAudit(
            Guid.CreateVersion7(),
            userId.ToString());

        return email;
    }
}
