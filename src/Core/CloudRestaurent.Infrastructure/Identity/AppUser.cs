using Microsoft.AspNetCore.Identity;

namespace CloudRestaurent.Infrastructure.Identity;

public class AppUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>
    /// Cap on the discount percentage this user may apply at the POS. Null = no cap
    /// (managers/admins). Cashiers should be configured with something like 5 or 10.
    /// </summary>
    public decimal? MaxDiscountPercent { get; set; }
}
