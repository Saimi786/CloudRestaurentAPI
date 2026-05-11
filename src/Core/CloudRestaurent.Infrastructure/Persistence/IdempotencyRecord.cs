namespace CloudRestaurent.Infrastructure.Persistence;

/// <summary>
/// Cached response for an `Idempotency-Key` header. Replaying the same key within the
/// retention window returns the original response — POS clients can safely re-send
/// after a network blip without double-billing or duplicating orders. Tied to UserId
/// so two cashiers can't accidentally collide on the same client-generated GUID.
/// </summary>
public class IdempotencyRecord
{
    public Guid Id { get; set; }
    public string Key { get; set; } = null!;
    public Guid? UserId { get; set; }
    public string Method { get; set; } = null!;
    public string Path { get; set; } = null!;
    public int StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? ContentType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>
/// Audit row recording every state-changing operation, with the source flag so we can
/// distinguish operations entered live from ones replayed from offline queues. The
/// foundation for future conflict resolution — without this audit there's nothing to
/// reconcile against.
/// </summary>
public class SyncOperation
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Method { get; set; } = null!;
    public string Path { get; set; } = null!;
    public string? ClientId { get; set; }              // device identifier from header
    public string? IdempotencyKey { get; set; }
    public SyncSource Source { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ClientOccurredAt { get; set; }   // when the client claims it happened
    public int StatusCode { get; set; }
}

public enum SyncSource
{
    /// <summary>Request hit the server live — not offline-replay.</summary>
    Live = 0,
    /// <summary>Client flagged this as a replay of a previously-offline operation.</summary>
    OfflineReplay = 1
}
