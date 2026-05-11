using System.Security.Claims;
using System.Text;
using CloudRestaurent.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Api.Common;

/// <summary>
/// Idempotency-Key handling for state-changing endpoints. Foundation for offline-first POS:
/// when the client retries a write after a network blip, we replay the cached response
/// instead of re-executing. Also logs every write to <see cref="SyncOperation"/> so future
/// sync work has a trail to reconcile against.
///
/// Headers respected:
///   Idempotency-Key   — client GUID/string; required for the cache to engage.
///   X-Client-Id       — optional device identifier, written to the sync log.
///   X-Sync-Source     — "OfflineReplay" tags this as a replay; default is Live.
///   X-Client-Occurred-At — RFC3339; when the client claims the op originally happened.
/// </summary>
public sealed class IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
{
    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    public async Task InvokeAsync(HttpContext ctx, AppDbContext db)
    {
        var method = ctx.Request.Method;
        if (!IsWrite(method)) { await next(ctx); return; }

        var key = ctx.Request.Headers.TryGetValue("Idempotency-Key", out var k) ? k.ToString() : null;
        var clientId = ctx.Request.Headers.TryGetValue("X-Client-Id", out var c) ? c.ToString() : null;
        var sourceHeader = ctx.Request.Headers.TryGetValue("X-Sync-Source", out var s) ? s.ToString() : null;
        var clientOccurredHeader = ctx.Request.Headers.TryGetValue("X-Client-Occurred-At", out var co) ? co.ToString() : null;

        var userId = TryGetUserId(ctx.User);
        var tenantId = TryGetTenantId(ctx.User);

        // 1) Replay cached response if a hit
        if (!string.IsNullOrWhiteSpace(key))
        {
            var existing = await db.Set<IdempotencyRecord>().AsNoTracking()
                .FirstOrDefaultAsync(r => r.Key == key && r.UserId == userId
                    && r.ExpiresAt > DateTimeOffset.UtcNow);
            if (existing is not null)
            {
                logger.LogInformation("Idempotency replay for key {Key} ({Method} {Path}) → {Status}",
                    key, existing.Method, existing.Path, existing.StatusCode);
                ctx.Response.StatusCode = existing.StatusCode;
                if (!string.IsNullOrEmpty(existing.ContentType))
                    ctx.Response.ContentType = existing.ContentType;
                // Headers MUST be set before WriteAsync — Kestrel locks the header dict
                // the moment the body starts flushing.
                ctx.Response.Headers.Append("Idempotent-Replay", "true");
                if (!string.IsNullOrEmpty(existing.ResponseBody))
                    await ctx.Response.WriteAsync(existing.ResponseBody);
                return;
            }
        }

        // 2) Capture the response body so we can cache it after a successful write
        var originalBody = ctx.Response.Body;
        await using var memStream = new MemoryStream();
        ctx.Response.Body = memStream;
        try
        {
            await next(ctx);
        }
        finally
        {
            memStream.Seek(0, SeekOrigin.Begin);
            await memStream.CopyToAsync(originalBody);
            ctx.Response.Body = originalBody;
        }

        // Re-resolve user/tenant after auth — handlers may have populated claims.
        userId = TryGetUserId(ctx.User);
        tenantId = TryGetTenantId(ctx.User);

        var status = ctx.Response.StatusCode;
        var isSuccess = status is >= 200 and < 300;

        // 3) Log the operation regardless of outcome (audit-track failures too)
        var source = string.Equals(sourceHeader, "OfflineReplay", StringComparison.OrdinalIgnoreCase)
            ? SyncSource.OfflineReplay : SyncSource.Live;
        DateTimeOffset? clientOccurred = null;
        if (DateTimeOffset.TryParse(clientOccurredHeader, out var coParsed)) clientOccurred = coParsed;

        db.Set<SyncOperation>().Add(new SyncOperation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Method = method,
            Path = ctx.Request.Path,
            ClientId = clientId,
            IdempotencyKey = key,
            Source = source,
            OccurredAt = DateTimeOffset.UtcNow,
            ClientOccurredAt = clientOccurred,
            StatusCode = status
        });

        // 4) Cache the response if successful + an idempotency key was provided
        if (isSuccess && !string.IsNullOrWhiteSpace(key))
        {
            memStream.Seek(0, SeekOrigin.Begin);
            string? body = null;
            if (memStream.Length > 0)
            {
                using var reader = new StreamReader(memStream, Encoding.UTF8, leaveOpen: true);
                body = await reader.ReadToEndAsync();
                memStream.Seek(0, SeekOrigin.Begin);
            }

            db.Set<IdempotencyRecord>().Add(new IdempotencyRecord
            {
                Id = Guid.NewGuid(),
                Key = key,
                UserId = userId,
                Method = method,
                Path = ctx.Request.Path,
                StatusCode = status,
                ResponseBody = body,
                ContentType = ctx.Response.ContentType,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.Add(Retention)
            });
        }

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException ex) { logger.LogWarning(ex, "Failed to persist sync/idempotency record."); }
    }

    private static bool IsWrite(string method) =>
        method is "POST" or "PUT" or "PATCH" or "DELETE";

    private static Guid? TryGetUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue("sub")
              ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static Guid? TryGetTenantId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue("tid");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
