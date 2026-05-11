using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Identity.Application.Users.Dtos;
using MediatR;

namespace CloudRestaurent.Modules.Identity.Application.Users.Queries;

public sealed record GetUsersQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<UserDto>>;

public sealed class GetUsersHandler(IIdentityService identity, ITenantContext tenantContext)
    : IRequestHandler<GetUsersQuery, IReadOnlyList<UserDto>>
{
    public async Task<IReadOnlyList<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var users = await identity.ListUsersAsync(tenantId, request.IncludeInactive, ct);
        return users
            .Select(u => new UserDto(u.Id, u.Email, u.FullName, u.IsActive, u.CreatedAt, u.LastLoginAt,
                u.Roles, u.BranchIds, u.MaxDiscountPercent))
            .ToList();
    }
}
