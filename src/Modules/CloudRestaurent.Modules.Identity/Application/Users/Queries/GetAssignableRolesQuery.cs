using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Identity.Application.Users.Dtos;
using MediatR;

namespace CloudRestaurent.Modules.Identity.Application.Users.Queries;

public sealed record GetAssignableRolesQuery : IRequest<IReadOnlyList<RoleDto>>;

public sealed class GetAssignableRolesHandler(IIdentityService identity)
    : IRequestHandler<GetAssignableRolesQuery, IReadOnlyList<RoleDto>>
{
    public Task<IReadOnlyList<RoleDto>> Handle(GetAssignableRolesQuery request, CancellationToken ct)
    {
        IReadOnlyList<RoleDto> result = identity.GetAssignableRoles()
            .Select(r => new RoleDto(r))
            .ToList();
        return Task.FromResult(result);
    }
}
