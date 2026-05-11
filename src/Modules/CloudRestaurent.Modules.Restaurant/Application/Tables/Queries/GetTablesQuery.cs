using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Restaurant.Application.Tables.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Restaurant.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Restaurant.Application.Tables.Queries;

public sealed record GetTablesQuery(
    Guid? BranchId = null,
    Guid? FloorPlanId = null,
    TableStatus? Status = null,
    bool IncludeInactive = false) : IRequest<IReadOnlyList<TableDto>>;

public sealed class GetTablesHandler(IAppDbContext db)
    : IRequestHandler<GetTablesQuery, IReadOnlyList<TableDto>>
{
    public async Task<IReadOnlyList<TableDto>> Handle(GetTablesQuery request, CancellationToken ct)
    {
        var tables = db.Set<RestaurantTable>().AsNoTracking();
        if (request.BranchId is { } bid) tables = tables.Where(t => t.BranchId == bid);
        if (request.FloorPlanId is { } fpid) tables = tables.Where(t => t.FloorPlanId == fpid);
        if (request.Status is { } st) tables = tables.Where(t => t.Status == st);
        if (!request.IncludeInactive) tables = tables.Where(t => t.IsActive);

        return await (
            from t in tables
            join fp in db.Set<FloorPlan>().AsNoTracking() on t.FloorPlanId equals fp.Id
            join br in db.Set<Branch>().AsNoTracking() on t.BranchId equals br.Id
            orderby br.Name, fp.DisplayOrder, t.Code
            select new TableDto(
                t.Id, t.FloorPlanId, fp.Name, t.BranchId, br.Name,
                t.Code, t.Capacity, t.Status, t.Status.ToString(), t.IsActive)
        ).ToListAsync(ct);
    }
}
