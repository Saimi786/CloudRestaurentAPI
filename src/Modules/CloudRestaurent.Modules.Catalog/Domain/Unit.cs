using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Catalog.Domain;

public class Unit : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public Guid GroupId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Multiplier expressing this unit in terms of its group's reference scale.
    /// A unit with factor = 1.0 is the "base" unit of the group.
    /// To convert qty from unit A to unit B (same group): qty * A.Factor / B.Factor.
    /// </summary>
    public decimal ConversionFactor { get; private set; }

    public bool IsActive { get; private set; }

    private Unit() { }

    public Unit(Guid id, Guid tenantId, Guid groupId, string code, string name, decimal conversionFactor)
    {
        if (conversionFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(conversionFactor),
                "Conversion factor must be greater than zero.");

        Id = id;
        TenantId = tenantId;
        GroupId = groupId;
        Code = code;
        Name = name;
        ConversionFactor = conversionFactor;
        IsActive = true;
    }

    public void Update(Guid groupId, string code, string name, decimal conversionFactor)
    {
        if (conversionFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(conversionFactor),
                "Conversion factor must be greater than zero.");

        GroupId = groupId;
        Code = code;
        Name = name;
        ConversionFactor = conversionFactor;
    }

    public bool IsBase => ConversionFactor == 1.0m;

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
