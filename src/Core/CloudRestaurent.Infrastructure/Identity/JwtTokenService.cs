using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CloudRestaurent.Application.Common.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CloudRestaurent.Infrastructure.Identity;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public AccessToken Issue(AuthenticatedUser user)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(TenantClaims.TenantId, user.TenantId.ToString()),
            new("name", user.FullName)
        };

        foreach (var role in user.Roles)
            claims.Add(new Claim("role", role));

        if (user.BranchIds.Count > 0)
            claims.Add(new Claim(TenantClaims.BranchIds, string.Join(",", user.BranchIds)));

        if (user.MaxDiscountPercent is { } cap)
            claims.Add(new Claim(TenantClaims.MaxDiscount, cap.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessToken(encoded, expires);
    }
}
