using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Catalog.Application.Units.Commands;

public sealed record DeactivateUnitCommand(Guid Id) : IRequest;

public sealed class DeactivateUnitHandler(IAppDbContext db) : IRequestHandler<DeactivateUnitCommand>
{
    public async Task Handle(DeactivateUnitCommand request, CancellationToken ct)
    {
        var unit = await db.Set<Unit>().FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new NotFoundException("Unit", request.Id);
        if (!unit.IsActive) return;

        if (await db.Set<Product>().AnyAsync(p => p.UnitId == request.Id && p.IsActive, ct))
            throw new BusinessRuleException(
                "Cannot deactivate a unit that is in use by active products.");

        unit.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
