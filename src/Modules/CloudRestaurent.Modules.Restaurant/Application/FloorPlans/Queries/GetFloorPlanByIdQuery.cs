using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Restaurant.Application.FloorPlans.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Restaurant.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Restaurant.Application.FloorPlans.Queries;

public sealed record GetFloorPlanByIdQuery(Guid Id) : IRequest<FloorPlanDto>;

public sealed class GetFloorPlanByIdHandler(IAppDbContext db)
    : IRequestHandler<GetFloorPlanByIdQuery, FloorPlanDto>
{
    public async Task<FloorPlanDto> Handle(GetFloorPlanByIdQuery request, CancellationToken ct)
    {
        var dto = await (
            from p in db.Set<FloorPlan>().AsNoTracking()
            join br in db.Set<Branch>().AsNoTracking() on p.BranchId equals br.Id
            where p.Id == request.Id
            select new FloorPlanDto(
                p.Id, p.BranchId, br.Name, p.Name, p.DisplayOrder,
                db.Set<RestaurantTable>().Count(t => t.FloorPlanId == p.Id),
                p.IsActive)
        ).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("FloorPlan", request.Id);
        return dto;
    }
}
