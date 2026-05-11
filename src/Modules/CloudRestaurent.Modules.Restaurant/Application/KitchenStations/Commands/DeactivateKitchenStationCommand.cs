using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Domain;
using CloudRestaurent.Modules.Restaurant.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Restaurant.Application.KitchenStations.Commands;

public sealed record DeactivateKitchenStationCommand(Guid Id) : IRequest;

public sealed class DeactivateKitchenStationHandler(IAppDbContext db)
    : IRequestHandler<DeactivateKitchenStationCommand>
{
    public async Task Handle(DeactivateKitchenStationCommand request, CancellationToken ct)
    {
        var station = await db.Set<KitchenStation>().FirstOrDefaultAsync(s => s.Id == request.Id, ct)
            ?? throw new NotFoundException("KitchenStation", request.Id);
        if (!station.IsActive) return;

        if (await db.Set<Category>().AnyAsync(c => c.KitchenStationId == request.Id && c.IsActive, ct))
            throw new BusinessRuleException(
                "Cannot deactivate a station that still has active categories routing to it. Reassign those categories first.");

        station.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
