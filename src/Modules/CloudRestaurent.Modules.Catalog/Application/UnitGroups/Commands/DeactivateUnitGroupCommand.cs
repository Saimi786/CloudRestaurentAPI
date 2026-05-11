using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainUnit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Catalog.Application.UnitGroups.Commands;

public sealed record DeactivateUnitGroupCommand(Guid Id) : IRequest;

public sealed class DeactivateUnitGroupHandler(IAppDbContext db)
    : IRequestHandler<DeactivateUnitGroupCommand>
{
    public async Task Handle(DeactivateUnitGroupCommand request, CancellationToken ct)
    {
        var group = await db.Set<UnitGroup>().FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new NotFoundException("UnitGroup", request.Id);
        if (!group.IsActive) return;

        if (await db.Set<DomainUnit>().AnyAsync(u => u.GroupId == request.Id && u.IsActive, ct))
            throw new BusinessRuleException(
                "Cannot deactivate a unit group that still has active units. Deactivate or move the units first.");

        group.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
