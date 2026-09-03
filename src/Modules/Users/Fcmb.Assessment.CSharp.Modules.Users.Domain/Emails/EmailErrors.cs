using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Modules.Users.Domain.Emails;

public static class EmailErrors
{
    public static readonly Error AlreadyExists =
        new("Email.AlreadyExists", "Email already exists");
}
