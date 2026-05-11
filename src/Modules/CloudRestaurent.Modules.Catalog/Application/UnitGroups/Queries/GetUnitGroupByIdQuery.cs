using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.UnitGroups.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.UnitGroups.Queries;

public sealed record GetUnitGroupByIdQuery(Guid Id) : IRequest<UnitGroupDto>;

public sealed class GetUnitGroupByIdHandler(IAppDbContext db)
    : IRequestHandler<GetUnitGroupByIdQuery, UnitGroupDto>
{
    public async Task<UnitGroupDto> Handle(GetUnitGroupByIdQuery request, CancellationToken ct)
    {
        var dto = await db.Set<UnitGroup>().AsNoTracking()
            .Where(g => g.Id == request.Id)
            .Select(g => new UnitGroupDto(
                g.Id, g.Name,
                db.Set<CloudRestaurent.Modules.Catalog.Domain.Unit>().Count(u => u.GroupId == g.Id),
                g.IsActive))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("UnitGroup", request.Id);
        return dto;
    }
}
