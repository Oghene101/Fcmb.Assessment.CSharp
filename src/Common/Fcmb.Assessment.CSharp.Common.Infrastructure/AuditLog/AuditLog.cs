namespace Fcmb.Assessment.CSharp.Common.Infrastructure.AuditLog;

public sealed class AuditLog
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Action { get; init; }
    public string UserId { get; init; }
    public string EntityName { get; init; }
    public Guid EntityId { get; init; }
    public DateTimeOffset OccurredOn { get; init; }
    public string Changes { get; init; }
}
