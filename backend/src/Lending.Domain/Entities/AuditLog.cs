namespace Lending.Domain.Entities;

public enum AuditAction
{
    Created,
    Updated,
    Deleted,
}

public class AuditLog
{
    public long Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? UserName { get; set; }
    public DateTime TimestampUtc { get; set; }
}
