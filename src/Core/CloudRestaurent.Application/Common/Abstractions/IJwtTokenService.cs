namespace CloudRestaurent.Application.Common.Abstractions;

public interface IJwtTokenService
{
    AccessToken Issue(AuthenticatedUser user);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
