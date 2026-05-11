namespace CloudRestaurent.Application.Common.Abstractions;

public static class TenantClaims
{
    public const string TenantId = "tid";

    /// <summary>Comma-separated branch ID GUIDs the user is assigned to. Empty for privileged roles.</summary>
    public const string BranchIds = "bids";

    /// <summary>Cap (percent) on discount the user can apply at the POS. Absent = unlimited.</summary>
    public const string MaxDiscount = "mxd";
}
