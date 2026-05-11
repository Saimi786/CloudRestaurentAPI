namespace CloudRestaurent.Application.Common.Abstractions;

/// <summary>
/// Read/write surface for the per-tenant role catalog: built-in roles (seeded once),
/// custom roles (created by tenant admins), and the permissions each role grants.
/// All operations are scoped to the calling tenant — built-in roles are returned for
/// reference (they're shared across tenants), but their names cannot be changed and
/// they cannot be deleted.
/// </summary>
public interface IRoleAdminService
{
    Task<IReadOnlyList<RoleDetailsDto>> ListAsync(Guid tenantId, CancellationToken ct);
    Task<RoleDetailsDto> CreateAsync(Guid tenantId, string name, IReadOnlyList<string> permissions, CancellationToken ct);
    Task<RoleDetailsDto> UpdateAsync(Guid tenantId, Guid roleId, string name, IReadOnlyList<string> permissions, CancellationToken ct);
    Task DeleteAsync(Guid tenantId, Guid roleId, CancellationToken ct);

    IReadOnlyList<PermissionDescriptor> GetPermissionCatalog();
}

public sealed record RoleDetailsDto(
    Guid Id,
    string Name,
    bool IsBuiltIn,
    int UserCount,
    IReadOnlyList<string> Permissions);

public sealed record PermissionDescriptor(string Key, string Area);
