using Fcmb.Assessment.CSharp.Common.Application.Data;
using Fcmb.Assessment.CSharp.Modules.Users.Domain.Users;

namespace Fcmb.Assessment.CSharp.Modules.Users.Application.Data;

public interface IUnitOfWork : ICommonUnitOfWork
{
    IRepository<User> UsersWriteRepository { get; }
}
