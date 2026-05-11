using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.Brands.Commands;

public sealed record DeactivateBrandCommand(Guid Id) : IRequest;

public sealed class DeactivateBrandHandler(IAppDbContext db) : IRequestHandler<DeactivateBrandCommand>
{
    public async Task Handle(DeactivateBrandCommand request, CancellationToken ct)
    {
        var brand = await db.Set<Brand>().FirstOrDefaultAsync(b => b.Id == request.Id, ct)
            ?? throw new NotFoundException("Brand", request.Id);
        if (!brand.IsActive) return;

        if (await db.Set<Product>().AnyAsync(p => p.BrandId == request.Id && p.IsActive, ct))
            throw new BusinessRuleException(
                "Cannot deactivate a brand that still has active products. Move or deactivate the products first.");

        brand.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
