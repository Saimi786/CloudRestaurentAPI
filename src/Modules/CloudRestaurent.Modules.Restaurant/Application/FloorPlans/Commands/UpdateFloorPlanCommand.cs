using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Restaurant.Application.FloorPlans.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Restaurant.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Restaurant.Application.FloorPlans.Commands;

public sealed record UpdateFloorPlanCommand(
    Guid Id,
    string Name,
    int DisplayOrder) : IRequest<FloorPlanDto>;

public sealed class UpdateFloorPlanValidator : AbstractValidator<UpdateFloorPlanCommand>
{
    public UpdateFloorPlanValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateFloorPlanHandler(IAppDbContext db)
    : IRequestHandler<UpdateFloorPlanCommand, FloorPlanDto>
{
    public async Task<FloorPlanDto> Handle(UpdateFloorPlanCommand request, CancellationToken ct)
    {
        var plan = await db.Set<FloorPlan>().FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new NotFoundException("FloorPlan", request.Id);

        if (await db.Set<FloorPlan>().AnyAsync(p =>
            p.Id != request.Id && p.BranchId == plan.BranchId && p.Name == request.Name, ct))
            throw new ConflictException(
                $"A floor plan named '{request.Name}' already exists in this branch.");

        plan.Update(request.Name.Trim(), request.DisplayOrder);
        await db.SaveChangesAsync(ct);

        var branch = await db.Set<Branch>().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == plan.BranchId, ct)
            ?? throw new NotFoundException("Branch", plan.BranchId);
        var tableCount = await db.Set<RestaurantTable>().CountAsync(t => t.FloorPlanId == plan.Id, ct);

        return new FloorPlanDto(plan.Id, plan.BranchId, branch.Name,
            plan.Name, plan.DisplayOrder, tableCount, plan.IsActive);
    }
}
