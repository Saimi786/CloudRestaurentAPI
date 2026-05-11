using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Restaurant.Application.Tables.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Restaurant.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Restaurant.Application.Tables.Queries;

public sealed record GetTableByIdQuery(Guid Id) : IRequest<TableDto>;

public sealed class GetTableByIdHandler(IAppDbContext db)
    : IRequestHandler<GetTableByIdQuery, TableDto>
{
    public async Task<TableDto> Handle(GetTableByIdQuery request, CancellationToken ct)
    {
        var dto = await (
            from t in db.Set<RestaurantTable>().AsNoTracking()
            join fp in db.Set<FloorPlan>().AsNoTracking() on t.FloorPlanId equals fp.Id
            join br in db.Set<Branch>().AsNoTracking() on t.BranchId equals br.Id
            where t.Id == request.Id
            select new TableDto(
                t.Id, t.FloorPlanId, fp.Name, t.BranchId, br.Name,
                t.Code, t.Capacity, t.Status, t.Status.ToString(), t.IsActive)
        ).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Table", request.Id);
        return dto;
    }
}
