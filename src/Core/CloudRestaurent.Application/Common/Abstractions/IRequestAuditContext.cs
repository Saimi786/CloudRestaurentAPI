namespace CloudRestaurent.Application.Common.Abstractions;

/// <summary>
/// Request-scoped audit metadata that flows alongside the regular DI scope so the
/// AppDbContext can stamp the originating HTTP path + idempotency key onto every
/// AuditEntry it writes. We keep this as a cross-cutting abstraction (not buried in
/// HttpContext) so background jobs and tests can populate it too — anywhere a
/// "logical request" exists, audit context can carry the same shape.
/// </summary>
public interface IRequestAuditContext
{
    string? RequestPath { get; }
    string? IdempotencyKey { get; }
}
