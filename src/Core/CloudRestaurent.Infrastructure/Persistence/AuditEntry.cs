namespace CloudRestaurent.Infrastructure.Persistence;

/// <summary>
/// One row per entity changed in a single SaveChangesAsync. The pair (BeforeJson, AfterJson)
/// captures only the columns that changed (or all columns on Add/Delete) — full snapshots
/// would be enormous on tables like OrderLine where each row has 20+ columns. Tying audit
/// to ChangeTracker rather than to handlers means every write — including bulk imports
/// and admin tools — is captured the same way, with no per-handler boilerplate.
/// </summary>
public class AuditEntry
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string EntityType { get; set; } = null!;
    public string EntityKey { get; set; } = null!;       // string repr of PK; composite keys joined by "|"
    public AuditChangeKind Kind { get; set; }
    public string? BeforeJson { get; set; }              // null for Added; populated for Modified/Deleted
    public string? AfterJson { get; set; }               // null for Deleted; populated for Added/Modified
    public string? RequestPath { get; set; }
    public string? IdempotencyKey { get; set; }
}

public enum AuditChangeKind
{
    Added = 0,
    Modified = 1,
    Deleted = 2
}
