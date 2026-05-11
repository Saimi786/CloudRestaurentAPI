namespace CloudRestaurent.Application.Common.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? UserName { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsInRole(string role);

    /// <summary>
    /// Branches the current user is assigned to. Empty means "no explicit scope" — privileged
    /// roles see every branch; non-privileged users with no rows here cannot see branch-keyed
    /// data at all (use <see cref="CanAccessAllBranches"/> to disambiguate).
    /// </summary>
    IReadOnlyList<Guid> BranchIds { get; }

    /// <summary>
    /// True when the user bypasses the branch filter (SuperAdmin or TenantAdmin). UI and
    /// query handlers should branch on this rather than checking role strings directly.
    /// </summary>
    bool CanAccessAllBranches { get; }

    /// <summary>
    /// Max discount percentage this user may apply at the POS, null = unlimited.
    /// </summary>
    decimal? MaxDiscountPercent { get; }
}
