namespace CloudRestaurent.Infrastructure.Identity;

/// <summary>
/// Assigns an <see cref="AppUser"/> to one or more <see cref="Domain.Companies.Branch"/>
/// outlets. The presence of any row scopes that user's data view to those branches —
/// queries against branch-keyed entities (orders, stock, cash registers, etc.) intersect
/// the requested branch with this set before returning rows.
///
/// Privileged roles (SuperAdmin, TenantAdmin) bypass this filter and see every branch
/// in the tenant whether or not they appear in this table.
/// </summary>
public class UserBranch
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
}
