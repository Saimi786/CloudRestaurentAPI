using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.Products.Commands;

public sealed record DeactivateProductCommand(Guid Id) : IRequest;

public sealed class DeactivateProductHandler(IAppDbContext db) : IRequestHandler<DeactivateProductCommand>
{
    public async Task Handle(DeactivateProductCommand request, CancellationToken ct)
    {
        var product = await db.Set<Product>().FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new NotFoundException("Product", request.Id);
        if (!product.IsActive) return;

        product.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
