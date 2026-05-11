using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Restaurant.Application.Tables.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Restaurant.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Restaurant.Application.Tables.Commands;

public sealed record UpdateTableCommand(
    Guid Id,
    Guid FloorPlanId,
    string Code,
    int Capacity) : IRequest<TableDto>;

public sealed class UpdateTableValidator : AbstractValidator<UpdateTableCommand>
{
    public UpdateTableValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FloorPlanId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(1);
    }
}

public sealed class UpdateTableHandler(IAppDbContext db) : IRequestHandler<UpdateTableCommand, TableDto>
{
    public async Task<TableDto> Handle(UpdateTableCommand request, CancellationToken ct)
    {
        var table = await db.Set<RestaurantTable>().FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new NotFoundException("Table", request.Id);

        var plan = await db.Set<FloorPlan>().FirstOrDefaultAsync(p => p.Id == request.FloorPlanId, ct)
            ?? throw new NotFoundException("FloorPlan", request.FloorPlanId);

        var branch = await db.Set<Branch>().FirstOrDefaultAsync(b => b.Id == plan.BranchId, ct)
            ?? throw new NotFoundException("Branch", plan.BranchId);

        if (await db.Set<RestaurantTable>().AnyAsync(t =>
            t.Id != request.Id && t.BranchId == plan.BranchId && t.Code == request.Code, ct))
            throw new ConflictException(
                $"A table with code '{request.Code}' already exists in this branch.");

        table.Update(plan.Id, plan.BranchId, request.Code.Trim(), request.Capacity);
        await db.SaveChangesAsync(ct);

        return new TableDto(table.Id, table.FloorPlanId, plan.Name,
            table.BranchId, branch.Name, table.Code, table.Capacity,
            table.Status, table.Status.ToString(), table.IsActive);
    }
}
