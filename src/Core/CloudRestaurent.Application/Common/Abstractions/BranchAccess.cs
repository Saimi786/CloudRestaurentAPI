using CloudRestaurent.Application.Common.Exceptions;

namespace CloudRestaurent.Application.Common.Abstractions;

/// <summary>
/// Helpers for query and command handlers that need to honor per-user branch scoping.
/// Privileged users (<see cref="ICurrentUser.CanAccessAllBranches"/>) bypass every check;
/// for everyone else we reject any operation that targets a branch outside their assignments.
///
/// Handlers should call <see cref="EnsureCanAccess"/> at the top of any write that takes
/// a BranchId, and <see cref="FilterAccessibleBranchIds"/> when materializing list queries
/// that the caller scopes manually.
/// </summary>
public static class BranchAccess
{
    public static void EnsureCanAccess(this ICurrentUser user, Guid branchId)
    {
        if (user.CanAccessAllBranches) return;
        if (!user.BranchIds.Contains(branchId))
            throw new ForbiddenException(
                "You are not assigned to this branch. Contact your administrator.");
    }

    /// <summary>
    /// If the caller is unrestricted, return the input set unchanged. Otherwise, intersect
    /// with the user's assigned branches — callers can apply the result as a `WHERE BranchId IN (...)`
    /// without further checks.
    /// </summary>
    public static IReadOnlyList<Guid> FilterAccessibleBranchIds(
        this ICurrentUser user, IReadOnlyList<Guid> requested)
    {
        if (user.CanAccessAllBranches) return requested;
        if (user.BranchIds.Count == 0) return Array.Empty<Guid>();
        return requested.Intersect(user.BranchIds).ToList();
    }
}
