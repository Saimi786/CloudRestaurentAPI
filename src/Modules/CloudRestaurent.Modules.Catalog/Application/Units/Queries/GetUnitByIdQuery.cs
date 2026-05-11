using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Units.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Catalog.Application.Units.Queries;

public sealed record GetUnitByIdQuery(Guid Id) : IRequest<UnitDto>;

public sealed class GetUnitByIdHandler(IAppDbContext db) : IRequestHandler<GetUnitByIdQuery, UnitDto>
{
    public async Task<UnitDto> Handle(GetUnitByIdQuery request, CancellationToken ct)
    {
        var dto = await (
            from u in db.Set<Unit>().AsNoTracking()
            join g in db.Set<UnitGroup>().AsNoTracking() on u.GroupId equals g.Id
            where u.Id == request.Id
            select new UnitDto(
                u.Id, u.GroupId, g.Name, u.Code, u.Name,
                u.ConversionFactor, u.ConversionFactor == 1.0m, u.IsActive)
        ).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Unit", request.Id);
        return dto;
    }
}
