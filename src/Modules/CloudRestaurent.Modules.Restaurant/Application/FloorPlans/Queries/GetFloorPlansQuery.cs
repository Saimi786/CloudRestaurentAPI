using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Restaurant.Application.FloorPlans.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Restaurant.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Restaurant.Application.FloorPlans.Queries;

public sealed record GetFloorPlansQuery(
    Guid? BranchId = null,
    bool IncludeInactive = false) : IRequest<IReadOnlyList<FloorPlanDto>>;

public sealed class GetFloorPlansHandler(IAppDbContext db)
    : IRequestHandler<GetFloorPlansQuery, IReadOnlyList<FloorPlanDto>>
{
    public async Task<IReadOnlyList<FloorPlanDto>> Handle(GetFloorPlansQuery request, CancellationToken ct)
    {
        var plans = db.Set<FloorPlan>().AsNoTracking();
        if (request.BranchId is { } bid) plans = plans.Where(p => p.BranchId == bid);
        if (!request.IncludeInactive) plans = plans.Where(p => p.IsActive);

        return await (
            from p in plans
            join br in db.Set<Branch>().AsNoTracking() on p.BranchId equals br.Id
            orderby br.Name, p.DisplayOrder, p.Name
            select new FloorPlanDto(
                p.Id, p.BranchId, br.Name, p.Name, p.DisplayOrder,
                db.Set<RestaurantTable>().Count(t => t.FloorPlanId == p.Id),
                p.IsActive)
        ).ToListAsync(ct);
    }
}
