using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Common.Application.Messaging;

public interface IDomainEventHandler<in T> where T : IDomainEvent
{
    Task<Result> Handle(T domainEvent, CancellationToken cancellationToken);
}
