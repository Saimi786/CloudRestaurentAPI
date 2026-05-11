using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Domain.Modifiers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Commands;

public sealed record DeactivateModifierGroupCommand(Guid Id) : IRequest;

public sealed class DeactivateModifierGroupHandler(IAppDbContext db)
    : IRequestHandler<DeactivateModifierGroupCommand>
{
    public async Task Handle(DeactivateModifierGroupCommand request, CancellationToken ct)
    {
        var group = await db.Set<ModifierGroup>().FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new NotFoundException("ModifierGroup", request.Id);

        if (!group.IsActive) return;

        // Drop any product links — when reactivated, the user re-attaches as needed.
        await db.Set<ProductModifierGroup>()
            .Where(p => p.ModifierGroupId == request.Id)
            .ExecuteDeleteAsync(ct);

        group.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
