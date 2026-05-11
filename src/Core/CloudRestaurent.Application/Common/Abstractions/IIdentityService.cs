namespace CloudRestaurent.Application.Common.Abstractions;

public interface IIdentityService
{
    Task<AuthenticatedUser?> ValidateCredentialsAsync(string email, string password, CancellationToken ct);

    Task<IReadOnlyList<UserSummary>> ListUsersAsync(Guid tenantId, bool includeInactive, CancellationToken ct);
    Task<UserSummary?> GetUserAsync(Guid id, Guid tenantId, CancellationToken ct);
    Task<UserSummary> CreateUserAsync(CreateUserInput input, CancellationToken ct);
    Task<UserSummary> UpdateUserAsync(Guid id, Guid tenantId, UpdateUserInput input, CancellationToken ct);
    Task ResetPasswordAsync(Guid id, Guid tenantId, string newPassword, CancellationToken ct);
    Task DeactivateUserAsync(Guid id, Guid tenantId, CancellationToken ct);

    Task SetBranchAssignmentsAsync(Guid userId, Guid tenantId, IReadOnlyList<Guid> branchIds, CancellationToken ct);

    /// <summary>
    /// SuperAdmin-only: list users assigned to a specific branch, regardless of which
    /// tenant they belong to. Used by the Manage Location page.
    /// </summary>
    Task<IReadOnlyList<UserSummary>> ListUsersByBranchAsync(Guid branchId, CancellationToken ct);

    /// <summary>
    /// SuperAdmin-only: reset any user's password regardless of which tenant they belong
    /// to. Used by the Manage Location page so the platform operator can rescue locked-out
    /// franchise admins without impersonation.
    /// </summary>
    Task ResetPasswordCrossTenantAsync(Guid userId, string newPassword, CancellationToken ct);

    IReadOnlyList<string> GetAssignableRoles();
}

public sealed record AuthenticatedUser(
    Guid UserId,
    string Email,
    string FullName,
    Guid TenantId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> BranchIds,
    decimal? MaxDiscountPercent);

public sealed record UserSummary(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> BranchIds,
    decimal? MaxDiscountPercent);

public sealed record CreateUserInput(
    string Email,
    string FullName,
    Guid TenantId,
    string Password,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> BranchIds,
    decimal? MaxDiscountPercent);

public sealed record UpdateUserInput(
    string FullName,
    bool IsActive,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> BranchIds,
    decimal? MaxDiscountPercent);

/// <summary>Thrown by Infrastructure when Identity rejects an op (bad password, dup email, etc.).</summary>
public sealed class IdentityOperationException(string message, IReadOnlyDictionary<string, string[]>? errors = null)
    : Exception(message)
{
    public IReadOnlyDictionary<string, string[]>? Errors { get; } = errors;
}
