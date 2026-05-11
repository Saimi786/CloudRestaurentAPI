using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Domain.Tenants;
using CloudRestaurent.Modules.Contacts.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CloudRestaurent.Modules.Sales.Infrastructure.Rewards;

/// <summary>
/// Background service that expires stale reward points once a day. Mirrors UP's
/// <c>pos:updateRewardPoints</c> artisan command — for each tenant with RP enabled and
/// a non-null expiry period, sum the <c>rp_earned</c> from sales older than the cutoff,
/// then move that delta (minus already-redeemed / already-expired) into the customer's
/// <c>TotalRewardPointsExpired</c> bucket.
///
/// Running this in-process via <see cref="BackgroundService"/> is fine while we have
/// one node. The moment we scale out we'll need leader-election (e.g. a DB-row lock or
/// Hangfire) so two workers don't double-expire.
/// </summary>
public sealed class RewardPointsExpiryJob(
    IServiceScopeFactory scopeFactory,
    ILogger<RewardPointsExpiryJob> logger,
    TimeProvider time) : BackgroundService
{
    private static readonly TimeSpan Cadence = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run on startup once, then daily. Catch & log so a single failure doesn't kill the loop.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reward-points expiry pass failed; will retry next cycle.");
            }

            try
            {
                await Task.Delay(Cadence, time, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    internal async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var now = time.GetUtcNow();

        // Iterate per-tenant with IgnoreQueryFilters so the global TenantId filter doesn't
        // hide rows from other tenants while this is a single platform-wide job.
        var settingsList = await ((DbContext)db).Set<BusinessSettings>().IgnoreQueryFilters()
            .Where(s => s.RewardPointsEnabled && s.RewardPointsExpiryPeriod != null)
            .ToListAsync(ct);

        if (settingsList.Count == 0)
        {
            logger.LogDebug("No tenants with reward-points expiry configured.");
            return;
        }

        var expiredTotal = 0;
        foreach (var settings in settingsList)
        {
            var cutoff = settings.RewardPointsExpiryUnit switch
            {
                RewardPointsExpiryUnit.Day => now.AddDays(-(settings.RewardPointsExpiryPeriod ?? 0)),
                RewardPointsExpiryUnit.Month => now.AddMonths(-(settings.RewardPointsExpiryPeriod ?? 0)),
                RewardPointsExpiryUnit.Year => now.AddYears(-(settings.RewardPointsExpiryPeriod ?? 0)),
                _ => now
            };

            expiredTotal += await ExpireForTenantAsync(db, settings.TenantId, cutoff, ct);
        }

        if (expiredTotal > 0)
            logger.LogInformation(
                "Reward-points expiry pass complete — {Count} customer rows updated.", expiredTotal);
    }

    private static async Task<int> ExpireForTenantAsync(
        IAppDbContext db, Guid tenantId, DateTimeOffset cutoff, CancellationToken ct)
    {
        // Sum rp_earned per customer for orders older than the cutoff. The delta to expire
        // is (total_earned_stale - already_used - already_expired) — never negative.
        var staleEarned = await ((DbContext)db).Set<Order>().IgnoreQueryFilters().AsNoTracking()
            .Where(o => o.TenantId == tenantId
                && o.CustomerId != null
                && o.Status == OrderStatus.Closed
                && o.ClosedAt != null && o.ClosedAt < cutoff
                && o.RewardPointsEarned > 0)
            .GroupBy(o => o.CustomerId!.Value)
            .Select(g => new { CustomerId = g.Key, Earned = g.Sum(o => o.RewardPointsEarned) })
            .ToListAsync(ct);

        if (staleEarned.Count == 0) return 0;

        var customerIds = staleEarned.Select(x => x.CustomerId).ToList();
        var customers = await ((DbContext)db).Set<Customer>().IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId && customerIds.Contains(c.Id))
            .ToListAsync(ct);

        var earnedByCustomer = staleEarned.ToDictionary(x => x.CustomerId, x => x.Earned);
        var touched = 0;
        foreach (var c in customers)
        {
            if (!earnedByCustomer.TryGetValue(c.Id, out var earned)) continue;
            var unaccounted = earned - c.TotalRewardPointsUsed - c.TotalRewardPointsExpired;
            if (unaccounted <= 0) continue;
            c.ExpirePoints(unaccounted);
            touched++;
        }

        if (touched > 0) await db.SaveChangesAsync(ct);
        return touched;
    }
}
