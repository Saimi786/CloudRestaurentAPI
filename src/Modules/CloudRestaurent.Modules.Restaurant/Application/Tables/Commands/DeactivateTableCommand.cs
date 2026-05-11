using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Restaurant.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Restaurant.Application.Tables.Commands;

public sealed record DeactivateTableCommand(Guid Id) : IRequest;

public sealed class DeactivateTableHandler(IAppDbContext db) : IRequestHandler<DeactivateTableCommand>
{
    public async Task Handle(DeactivateTableCommand request, CancellationToken ct)
    {
        var table = await db.Set<RestaurantTable>().FirstOrDefaultAsync(t => t.Id == request.Id, ct)
            ?? throw new NotFoundException("Table", request.Id);
        if (!table.IsActive) return;

        table.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
