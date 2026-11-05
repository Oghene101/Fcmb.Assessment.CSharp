using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;

public static class UserErrors
{
    public static Error UnsupportedUserType(UserType userType) =>
        Error.Problem("User.UnsupportedUserType", $"User type '{userType}' has no integration counterpart.");
}
