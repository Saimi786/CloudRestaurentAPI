using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Catalog.Application.UnitGroups.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.UnitGroups.Queries;

public sealed record GetUnitGroupsQuery(bool IncludeInactive = false)
    : IRequest<IReadOnlyList<UnitGroupDto>>;

public sealed class GetUnitGroupsHandler(IAppDbContext db)
    : IRequestHandler<GetUnitGroupsQuery, IReadOnlyList<UnitGroupDto>>
{
    public async Task<IReadOnlyList<UnitGroupDto>> Handle(GetUnitGroupsQuery request, CancellationToken ct)
    {
        var groups = db.Set<UnitGroup>().AsNoTracking();
        if (!request.IncludeInactive) groups = groups.Where(g => g.IsActive);

        return await groups
            .OrderBy(g => g.Name)
            .Select(g => new UnitGroupDto(
                g.Id,
                g.Name,
                db.Set<CloudRestaurent.Modules.Catalog.Domain.Unit>().Count(u => u.GroupId == g.Id),
                g.IsActive))
            .ToListAsync(ct);
    }
}
