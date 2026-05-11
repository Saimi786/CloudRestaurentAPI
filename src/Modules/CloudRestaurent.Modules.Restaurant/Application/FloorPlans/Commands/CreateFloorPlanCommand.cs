using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Restaurant.Application.FloorPlans.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Restaurant.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Restaurant.Application.FloorPlans.Commands;

public sealed record CreateFloorPlanCommand(
    Guid BranchId,
    string Name,
    int DisplayOrder) : IRequest<FloorPlanDto>;

public sealed class CreateFloorPlanValidator : AbstractValidator<CreateFloorPlanCommand>
{
    public CreateFloorPlanValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateFloorPlanHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<CreateFloorPlanCommand, FloorPlanDto>
{
    public async Task<FloorPlanDto> Handle(CreateFloorPlanCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var branch = await db.Set<Branch>().FirstOrDefaultAsync(b => b.Id == request.BranchId, ct)
            ?? throw new NotFoundException("Branch", request.BranchId);

        if (await db.Set<FloorPlan>().AnyAsync(p => p.BranchId == request.BranchId && p.Name == request.Name, ct))
            throw new ConflictException(
                $"A floor plan named '{request.Name}' already exists in this branch.");

        var plan = new FloorPlan(Guid.NewGuid(), tenantId, request.BranchId,
            request.Name.Trim(), request.DisplayOrder);
        db.Set<FloorPlan>().Add(plan);
        await db.SaveChangesAsync(ct);

        return new FloorPlanDto(plan.Id, plan.BranchId, branch.Name,
            plan.Name, plan.DisplayOrder, 0, plan.IsActive);
    }
}
