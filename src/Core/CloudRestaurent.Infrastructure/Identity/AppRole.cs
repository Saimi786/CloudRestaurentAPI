using Microsoft.AspNetCore.Identity;

namespace CloudRestaurent.Infrastructure.Identity;

public class AppRole : IdentityRole<Guid>
{
    public Guid? TenantId { get; set; }

    public AppRole() { }
    public AppRole(string name) : base(name) { }
}
