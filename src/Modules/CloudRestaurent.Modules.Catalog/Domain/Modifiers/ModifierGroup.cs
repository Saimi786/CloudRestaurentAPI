using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Catalog.Domain.Modifiers;

public class ModifierGroup : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = null!;

    /// <summary>If true, the customer must pick at least <see cref="MinSelect"/> modifiers (≥1).</summary>
    public bool IsRequired { get; private set; }
    public int MinSelect { get; private set; }
    public int MaxSelect { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<Modifier> _modifiers = new();
    public IReadOnlyCollection<Modifier> Modifiers => _modifiers;

    private ModifierGroup() { }

    public ModifierGroup(Guid id, Guid tenantId, string name, bool isRequired, int minSelect, int maxSelect)
    {
        if (maxSelect < 1)
            throw new ArgumentOutOfRangeException(nameof(maxSelect), "MaxSelect must be at least 1.");
        if (minSelect < 0 || minSelect > maxSelect)
            throw new ArgumentOutOfRangeException(nameof(minSelect), "MinSelect must be between 0 and MaxSelect.");
        if (isRequired && minSelect < 1)
            throw new ArgumentException("Required groups must have MinSelect ≥ 1.", nameof(minSelect));

        Id = id;
        TenantId = tenantId;
        Name = name;
        IsRequired = isRequired;
        MinSelect = minSelect;
        MaxSelect = maxSelect;
        IsActive = true;
    }

    public void Update(string name, bool isRequired, int minSelect, int maxSelect)
    {
        if (maxSelect < 1)
            throw new ArgumentOutOfRangeException(nameof(maxSelect));
        if (minSelect < 0 || minSelect > maxSelect)
            throw new ArgumentOutOfRangeException(nameof(minSelect));
        if (isRequired && minSelect < 1)
            throw new ArgumentException("Required groups must have MinSelect ≥ 1.", nameof(minSelect));

        Name = name;
        IsRequired = isRequired;
        MinSelect = minSelect;
        MaxSelect = maxSelect;
    }

    public void ReplaceModifiers(IEnumerable<Modifier> modifiers)
    {
        _modifiers.Clear();
        foreach (var m in modifiers) _modifiers.Add(m);
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
