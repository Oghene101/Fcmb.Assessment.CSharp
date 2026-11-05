using Fcmb.Assessment.CSharp.Common.Application.Data;

namespace Fcmb.Assessment.CSharp.Common.Infrastructure.Messaging;

// Tells the shared outbox and inbox machinery which module a unit of work belongs to.
public sealed record MessagingModule<TUnitOfWork>(string Name)
    where TUnitOfWork : ICommonUnitOfWork;
