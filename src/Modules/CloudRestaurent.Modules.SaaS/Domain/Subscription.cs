using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.SaaS.Domain;

public enum SubscriptionStatus
{
    /// <summary>Tenant requested upgrade — waiting on platform approval.</summary>
    PendingApproval = 0,
    /// <summary>Active and billing.</summary>
    Active = 1,
    /// <summary>Free trial — converts to Active or Cancelled.</summary>
    Trial = 2,
    /// <summary>Past-due / suspended.</summary>
    Suspended = 3,
    /// <summary>Cancelled by tenant or platform.</summary>
    Cancelled = 4
}

/// <summary>
/// Tracks a tenant's relationship to a Package. Manual approval flow per locked decision —
/// no online payment. Tenants request via <see cref="Request"/>; SuperAdmin approves via
/// <see cref="Approve"/> and the platform admin records the offline payment.
/// </summary>
public class Subscription : AuditableEntity<Guid>
{
    public Guid TenantId { get; private set; }
    public Guid PackageId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public DateOnly? NextBillingDate { get; private set; }
    public Guid? CouponId { get; private set; }
    public decimal? AppliedDiscountPercent { get; private set; }
    public Guid? RequestedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public string? Notes { get; private set; }

    private Subscription() { }

    /// <summary>Tenant requests an upgrade. Lands in PendingApproval.</summary>
    public static Subscription Request(
        Guid id, Guid tenantId, Guid packageId, Guid? couponId, decimal? discountPercent,
        Guid requestedByUserId, string? notes)
    {
        return new Subscription
        {
            Id = id,
            TenantId = tenantId,
            PackageId = packageId,
            CouponId = couponId,
            AppliedDiscountPercent = discountPercent,
            RequestedByUserId = requestedByUserId,
            Notes = notes,
            Status = SubscriptionStatus.PendingApproval
        };
    }

    /// <summary>Platform admin approves; tenant becomes Active starting today.</summary>
    public void Approve(Guid approvedByUserId, DateOnly startDate, DateOnly nextBillingDate)
    {
        if (Status is not (SubscriptionStatus.PendingApproval or SubscriptionStatus.Trial))
            throw new InvalidOperationException($"Cannot approve subscription in status {Status}.");
        Status = SubscriptionStatus.Active;
        StartDate = startDate;
        NextBillingDate = nextBillingDate;
        ApprovedByUserId = approvedByUserId;
        ApprovedAt = DateTimeOffset.UtcNow;
    }

    public void Suspend(string? reason)
    {
        if (Status != SubscriptionStatus.Active)
            throw new InvalidOperationException($"Only Active subscriptions can be suspended (was {Status}).");
        Status = SubscriptionStatus.Suspended;
        if (!string.IsNullOrWhiteSpace(reason)) Notes = reason;
    }

    public void Cancel(DateOnly endDate, string? reason)
    {
        if (Status is SubscriptionStatus.Cancelled)
            return;
        Status = SubscriptionStatus.Cancelled;
        EndDate = endDate;
        if (!string.IsNullOrWhiteSpace(reason)) Notes = reason;
    }

    public void RecordRenewal(DateOnly nextBillingDate) => NextBillingDate = nextBillingDate;
}
