namespace Fcmb.Assessment.CSharp.Common.Domain;

public abstract record DomainEvent(
    Guid Id,
    DateTimeOffset OccurredOn) : IDomainEvent;
