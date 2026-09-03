using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Modules.Users.Domain.PhoneNumbers;

public static class PhoneNumberErrors
{
    public static readonly Error AlreadyExists =
        new("PhoneNumber.AlreadyExists", "PhoneNumber already exists");
}
