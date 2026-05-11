using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Identity.Application.Users.Dtos;
using MediatR;

namespace CloudRestaurent.Modules.Identity.Application.Users.Queries;

public sealed record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;

public sealed class GetUserByIdHandler(IIdentityService identity, ITenantContext tenantContext)
    : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var u = await identity.GetUserAsync(request.Id, tenantId, ct)
            ?? throw new NotFoundException("User", request.Id);

        return new UserDto(u.Id, u.Email, u.FullName, u.IsActive, u.CreatedAt, u.LastLoginAt,
            u.Roles, u.BranchIds, u.MaxDiscountPercent);
    }
}
