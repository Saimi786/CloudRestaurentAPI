namespace CloudRestaurent.Infrastructure.Identity;

/// <summary>
/// Many-to-many: which permissions a role grants. Seeded from <see cref="AppPermissions.DefaultsByRole"/>;
/// tenant admins can edit at runtime to fine-tune.
/// </summary>
public class RolePermission
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string Permission { get; set; } = null!;
}
