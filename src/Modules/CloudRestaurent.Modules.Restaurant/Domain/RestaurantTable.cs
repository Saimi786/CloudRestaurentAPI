using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Restaurant.Domain;

/// <summary>
/// A physical table at a Branch. "RestaurantTable" rather than "Table" to avoid
/// the SQL keyword and the "Table" overload from EF / Db terminology.
/// </summary>
public class RestaurantTable : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid FloorPlanId { get; private set; }

    /// <summary>Denormalized from FloorPlan for cheap branch-scoped queries.</summary>
    public Guid BranchId { get; private set; }

    public string Code { get; private set; } = null!;
    public int Capacity { get; private set; }
    public TableStatus Status { get; private set; }
    public bool IsActive { get; private set; }

    private RestaurantTable() { }

    public RestaurantTable(
        Guid id, Guid tenantId, Guid floorPlanId, Guid branchId,
        string code, int capacity)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be ≥ 1.");

        Id = id;
        TenantId = tenantId;
        FloorPlanId = floorPlanId;
        BranchId = branchId;
        Code = code;
        Capacity = capacity;
        Status = TableStatus.Available;
        IsActive = true;
    }

    public void Update(Guid floorPlanId, Guid branchId, string code, int capacity)
    {
        if (capacity < 1)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        FloorPlanId = floorPlanId;
        BranchId = branchId;
        Code = code;
        Capacity = capacity;
    }

    public void SetStatus(TableStatus status) => Status = status;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
