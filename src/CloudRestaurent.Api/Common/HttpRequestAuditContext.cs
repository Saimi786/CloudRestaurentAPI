using CloudRestaurent.Application.Common.Abstractions;

namespace CloudRestaurent.Api.Common;

/// <summary>
/// HTTP-pipeline binding for <see cref="IRequestAuditContext"/>. Reads the path and
/// any Idempotency-Key header straight off the current request. Registered scoped
/// so each request gets its own snapshot — no AsyncLocal flow required since
/// IHttpContextAccessor already gives us per-request scoping for free.
/// </summary>
public sealed class HttpRequestAuditContext(IHttpContextAccessor accessor) : IRequestAuditContext
{
    public string? RequestPath => accessor.HttpContext?.Request.Path.Value;

    public string? IdempotencyKey =>
        accessor.HttpContext?.Request.Headers.TryGetValue("Idempotency-Key", out var v) == true
            ? v.ToString()
            : null;
}
