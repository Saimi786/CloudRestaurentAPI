using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Infrastructure.Identity;

namespace CloudRestaurent.Api.Common;

public sealed class TenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public Guid? TenantId
    {
        get
        {
            var raw = httpContextAccessor.HttpContext?.User.FindFirstValue(TenantClaims.TenantId);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var raw = httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string? UserName =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? httpContextAccessor.HttpContext?.User.Identity?.Name;

    public IReadOnlyList<string> Roles =>
        httpContextAccessor.HttpContext?.User
            .FindAll("role")
            .Select(c => c.Value)
            .ToList() ?? [];

    public bool IsInRole(string role) =>
        httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;

    public IReadOnlyList<Guid> BranchIds
    {
        get
        {
            var raw = httpContextAccessor.HttpContext?.User.FindFirstValue(TenantClaims.BranchIds);
            if (string.IsNullOrEmpty(raw)) return Array.Empty<Guid>();
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToList();
        }
    }

    public bool CanAccessAllBranches =>
        IsInRole(AppRoles.SuperAdmin) || IsInRole(AppRoles.TenantAdmin);

    public decimal? MaxDiscountPercent
    {
        get
        {
            var raw = httpContextAccessor.HttpContext?.User.FindFirstValue(TenantClaims.MaxDiscount);
            return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
        }
    }
}
