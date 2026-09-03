namespace Fcmb.Assessment.CSharp.Modules.Users.Domain.PhoneNumbers;

public interface IPhoneNumberRepository
{
    Task<bool> PhoneNumberExistsAsync(string number);
}
