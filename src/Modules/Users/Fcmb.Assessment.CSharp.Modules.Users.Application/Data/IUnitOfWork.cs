using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Emails;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.PhoneNumbers;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;

namespace Fcmb.Assessment.CSharp.Modules.Users.Application.Data;

public interface IUnitOfWork : ICommonUnitOfWork
{
    IRepository<User> UsersWriteRepository { get; }

    IRepository<Email> EmailsWriteRepository { get; }
    IEmailRepository EmailsReadRepository { get; }

    IRepository<PhoneNumber> PhoneNumbersWriteRepository { get; }
    IPhoneNumberRepository PhoneNumbersReadRepository { get; }
}
