namespace Fcmb.Assessment.CSharp.Common.Domain;

public interface IDomainEvent
{
    Guid Id { get; }
    DateTimeOffset OccurredOn { get; }
}
