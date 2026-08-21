using System.ComponentModel.DataAnnotations;

namespace Fcmb.Assessment.CSharp.Common.Domain;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    [Required] public Guid Id { get; private set; }
    [Required] public DateTimeOffset CreatedAt { get; private set; }
    [Required, MaxLength(150)] public string CreatedBy { get; private set; }
    [Required] public DateTimeOffset LastUpdatedAt { get; private set; }
    [Required, MaxLength(150)] public string LastUpdatedBy { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    [MaxLength(150)] public string? DeletedBy { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    protected void InitializeAudit(Guid id, string createdBy)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Id = id;
        CreatedAt = now;
        CreatedBy = createdBy;
        LastUpdatedAt = now;
        LastUpdatedBy = createdBy;
        DeletedAt = null;
    }

    protected void UpdateAudit(string updatedBy)
    {
        LastUpdatedAt = DateTimeOffset.UtcNow;
        LastUpdatedBy = updatedBy;
    }

}
