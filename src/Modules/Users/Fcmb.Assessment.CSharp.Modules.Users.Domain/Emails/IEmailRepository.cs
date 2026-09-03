namespace Fcmb.Assessment.CSharp.Modules.Users.Domain.Emails;

public interface IEmailRepository
{
    Task<bool> EmailExistsAsync(string address);
}
