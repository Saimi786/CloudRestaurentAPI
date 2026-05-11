using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;

namespace CloudRestaurent.Modules.Catalog.Application.Products.Commands;

/// <summary>
/// "86" / un-86 a Product without going through full edit. Common FOH operation:
/// kitchen runs out of an ingredient, manager toggles it off the menu in one tap.
/// </summary>
public sealed record SetProductAvailabilityCommand(Guid Id, bool IsAvailable) : IRequest;

public sealed class SetProductAvailabilityHandler(IAppDbContext db)
    : IRequestHandler<SetProductAvailabilityCommand>
{
    public async Task Handle(SetProductAvailabilityCommand request, CancellationToken ct)
    {
        var product = await db.Set<Product>().FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new NotFoundException("Product", request.Id);
        product.SetAvailability(request.IsAvailable);
        await db.SaveChangesAsync(ct);
    }
}
