using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Pricing.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Pricing.Application.MixMatch.Commands;

public sealed record DeactivateMixMatchGroupCommand(Guid Id) : IRequest;

public sealed class DeactivateMixMatchGroupHandler(IAppDbContext db) : IRequestHandler<DeactivateMixMatchGroupCommand>
{
    public async Task Handle(DeactivateMixMatchGroupCommand request, CancellationToken ct)
    {
        var group = await db.Set<MixMatchGroup>().FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new NotFoundException("MixMatchGroup", request.Id);
        if (!group.IsActive) return;
        group.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
