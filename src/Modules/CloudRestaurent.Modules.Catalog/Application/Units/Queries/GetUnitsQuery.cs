using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Catalog.Application.Units.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Catalog.Application.Units.Queries;

public sealed record GetUnitsQuery(
    Guid? GroupId = null,
    bool IncludeInactive = false) : IRequest<IReadOnlyList<UnitDto>>;

public sealed class GetUnitsHandler(IAppDbContext db)
    : IRequestHandler<GetUnitsQuery, IReadOnlyList<UnitDto>>
{
    public async Task<IReadOnlyList<UnitDto>> Handle(GetUnitsQuery request, CancellationToken ct)
    {
        var units = db.Set<Unit>().AsNoTracking();
        if (request.GroupId is { } gid) units = units.Where(u => u.GroupId == gid);
        if (!request.IncludeInactive) units = units.Where(u => u.IsActive);

        return await (
            from u in units
            join g in db.Set<UnitGroup>().AsNoTracking() on u.GroupId equals g.Id
            orderby g.Name, u.ConversionFactor
            select new UnitDto(
                u.Id, u.GroupId, g.Name, u.Code, u.Name,
                u.ConversionFactor, u.ConversionFactor == 1.0m, u.IsActive)
        ).ToListAsync(ct);
    }
}
