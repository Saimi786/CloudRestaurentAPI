namespace CloudRestaurent.Modules.Identity.Application.Users.Dtos;

public sealed record UserDto(
    Guid Id,
    string Email,
    string FullName,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> BranchIds,
    decimal? MaxDiscountPercent);

public sealed record RoleDto(string Name);
