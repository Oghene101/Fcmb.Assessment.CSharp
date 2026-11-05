using Fcmb.Assessment.CSharp.Common.Domain;

namespace Fcmb.Assessment.CSharp.Common.Application.Messaging;

public interface IIntegrationEventHandler<in T>
{
    Task<Result> Handle(T integrationEvent, CancellationToken cancellationToken);
}
