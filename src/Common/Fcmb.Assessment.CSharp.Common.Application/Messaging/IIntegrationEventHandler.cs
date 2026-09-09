namespace Fcmb.Assessment.CSharp.Common.Application.Messaging;

public interface IIntegrationEventHandler<in T>
{
    Task Handle(T integrationEvent, CancellationToken cancellationToken);
}
