namespace Fcmb.Assessment.CSharp.Common.Application.Messaging;

public interface IIntegrationEvent
{
    Guid Id { get; }

    DateTimeOffset OccurredOn { get; }
}
