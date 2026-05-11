using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CloudRestaurent.Infrastructure.Identity;

/// <summary>
/// Builds a policy on the fly for any name starting with "perm:" — splits on "|" to pull
/// out the OR-list of permissions and turns it into a <see cref="PermissionRequirement"/>.
/// Avoids having to register every policy by hand at startup.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var perms = policyName[HasPermissionAttribute.PolicyPrefix.Length..]
                .Split('|', StringSplitOptions.RemoveEmptyEntries);
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(perms))
                .Build();
        }
        return await base.GetPolicyAsync(policyName);
    }
}
