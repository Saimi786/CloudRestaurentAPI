using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Restaurant.Domain;

public enum KitchenTicketStatus
{
    Pending   = 0,   // sent to kitchen, not yet picked up
    Preparing = 1,   // being cooked
    Ready     = 2,   // ready for service
    Served    = 3    // delivered to customer / closed
}

public class KitchenTicket : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; private set; }
    public Guid BranchId { get; private set; }
    public KitchenTicketStatus Status { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public DateTimeOffset? ReadyAt { get; private set; }
    public DateTimeOffset? ServedAt { get; private set; }

    /// <summary>
    /// Comma-separated GUIDs of stations that have bumped (marked their portion ready).
    /// Stored as a string to keep the schema simple — lists are short (≤ ~6 stations per ticket).
    /// </summary>
    public string? BumpedStationsRaw { get; private set; }

    private KitchenTicket() { }

    public KitchenTicket(Guid id, Guid tenantId, Guid orderId, Guid branchId)
    {
        Id = id;
        TenantId = tenantId;
        OrderId = orderId;
        BranchId = branchId;
        Status = KitchenTicketStatus.Pending;
        OpenedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Allowed transitions: Pending → Preparing → Ready → Served.
    /// Backwards moves are rejected (kitchen tickets are forward-only in v1).
    /// </summary>
    public void Advance(KitchenTicketStatus next)
    {
        if (next <= Status)
            throw new InvalidOperationException(
                $"Cannot move ticket from {Status} to {next}. Status is forward-only.");

        Status = next;
        if (next == KitchenTicketStatus.Ready) ReadyAt = DateTimeOffset.UtcNow;
        if (next == KitchenTicketStatus.Served) ServedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Force-mark the ticket as Served. Used when the parent Order is closed.</summary>
    public void MarkServed()
    {
        if (Status == KitchenTicketStatus.Served) return;
        Status = KitchenTicketStatus.Served;
        ServedAt = DateTimeOffset.UtcNow;
    }

    public IReadOnlyList<Guid> GetBumpedStations()
    {
        if (string.IsNullOrEmpty(BumpedStationsRaw)) return Array.Empty<Guid>();
        return BumpedStationsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(Guid.Parse).ToList();
    }

    public bool IsStationBumped(Guid stationId) =>
        !string.IsNullOrEmpty(BumpedStationsRaw) &&
        BumpedStationsRaw.Contains(stationId.ToString());

    public void BumpStation(Guid stationId)
    {
        if (IsStationBumped(stationId)) return;
        BumpedStationsRaw = string.IsNullOrEmpty(BumpedStationsRaw)
            ? stationId.ToString()
            : $"{BumpedStationsRaw},{stationId}";
    }

    public void UnbumpStation(Guid stationId)
    {
        var current = GetBumpedStations().Where(s => s != stationId).ToList();
        BumpedStationsRaw = current.Count == 0 ? null : string.Join(",", current);
    }

    /// <summary>
    /// True when the supplied set of involved stations are all bumped. Caller passes the
    /// distinct stations the ticket actually touches (derived from line categories).
    /// </summary>
    public bool AreAllStationsBumped(IReadOnlyCollection<Guid> involvedStations)
    {
        if (involvedStations.Count == 0) return false;
        var bumped = new HashSet<Guid>(GetBumpedStations());
        return involvedStations.All(bumped.Contains);
    }
}
