using Microsoft.AspNetCore.Authorization;

namespace CloudRestaurent.Infrastructure.Identity;

/// <summary>
/// Endpoint guard: caller must have at least one of the listed permissions (OR-semantics).
/// Resolves through <see cref="PermissionAuthorizationHandler"/> against the role-permission table.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "perm:";

    public HasPermissionAttribute(params string[] permissions)
    {
        if (permissions.Length == 0)
            throw new ArgumentException("At least one permission required.", nameof(permissions));
        Policy = PolicyPrefix + string.Join("|", permissions);
    }
}

public sealed class PermissionRequirement(string[] permissions) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; } = permissions;
}
