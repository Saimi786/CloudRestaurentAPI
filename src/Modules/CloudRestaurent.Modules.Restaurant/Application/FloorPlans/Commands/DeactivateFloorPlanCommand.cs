using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Restaurant.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Restaurant.Application.FloorPlans.Commands;

public sealed record DeactivateFloorPlanCommand(Guid Id) : IRequest;

public sealed class DeactivateFloorPlanHandler(IAppDbContext db)
    : IRequestHandler<DeactivateFloorPlanCommand>
{
    public async Task Handle(DeactivateFloorPlanCommand request, CancellationToken ct)
    {
        var plan = await db.Set<FloorPlan>().FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new NotFoundException("FloorPlan", request.Id);
        if (!plan.IsActive) return;

        if (await db.Set<RestaurantTable>().AnyAsync(t => t.FloorPlanId == request.Id && t.IsActive, ct))
            throw new BusinessRuleException(
                "Cannot deactivate a floor plan that still has active tables. Deactivate or move the tables first.");

        plan.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
