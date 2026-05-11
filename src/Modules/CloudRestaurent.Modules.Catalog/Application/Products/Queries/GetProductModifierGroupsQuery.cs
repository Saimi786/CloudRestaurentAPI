using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Dtos;
using CloudRestaurent.Modules.Catalog.Domain.Modifiers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;

namespace CloudRestaurent.Modules.Catalog.Application.Products.Queries;

public sealed record GetProductModifierGroupsQuery(Guid ProductId)
    : IRequest<IReadOnlyList<ModifierGroupSummaryDto>>;

public sealed class GetProductModifierGroupsHandler(IAppDbContext db)
    : IRequestHandler<GetProductModifierGroupsQuery, IReadOnlyList<ModifierGroupSummaryDto>>
{
    public async Task<IReadOnlyList<ModifierGroupSummaryDto>> Handle(GetProductModifierGroupsQuery request, CancellationToken ct)
    {
        if (!await db.Set<Product>().AnyAsync(p => p.Id == request.ProductId, ct))
            throw new NotFoundException("Product", request.ProductId);

        return await (
            from link in db.Set<ProductModifierGroup>().AsNoTracking()
            where link.ProductId == request.ProductId
            join g in db.Set<ModifierGroup>().AsNoTracking() on link.ModifierGroupId equals g.Id
            orderby link.DisplayOrder, g.Name
            select new ModifierGroupSummaryDto(
                g.Id, g.Name, g.IsRequired, g.MinSelect, g.MaxSelect,
                db.Set<Modifier>().Count(m => m.ModifierGroupId == g.Id),
                g.IsActive)
        ).ToListAsync(ct);
    }
}
