using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Restaurant.Application.Tables.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Restaurant.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Restaurant.Application.Tables.Commands;

public sealed record CreateTableCommand(
    Guid FloorPlanId,
    string Code,
    int Capacity) : IRequest<TableDto>;

public sealed class CreateTableValidator : AbstractValidator<CreateTableCommand>
{
    public CreateTableValidator()
    {
        RuleFor(x => x.FloorPlanId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(1);
    }
}

public sealed class CreateTableHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<CreateTableCommand, TableDto>
{
    public async Task<TableDto> Handle(CreateTableCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var plan = await db.Set<FloorPlan>().FirstOrDefaultAsync(p => p.Id == request.FloorPlanId, ct)
            ?? throw new NotFoundException("FloorPlan", request.FloorPlanId);

        var branch = await db.Set<Branch>().FirstOrDefaultAsync(b => b.Id == plan.BranchId, ct)
            ?? throw new NotFoundException("Branch", plan.BranchId);

        // Code unique within a branch (across floor plans).
        if (await db.Set<RestaurantTable>().AnyAsync(t => t.BranchId == plan.BranchId && t.Code == request.Code, ct))
            throw new ConflictException(
                $"A table with code '{request.Code}' already exists in this branch.");

        var table = new RestaurantTable(Guid.NewGuid(), tenantId,
            plan.Id, plan.BranchId, request.Code.Trim(), request.Capacity);
        db.Set<RestaurantTable>().Add(table);
        await db.SaveChangesAsync(ct);

        return new TableDto(table.Id, table.FloorPlanId, plan.Name,
            table.BranchId, branch.Name, table.Code, table.Capacity,
            table.Status, table.Status.ToString(), table.IsActive);
    }
}
