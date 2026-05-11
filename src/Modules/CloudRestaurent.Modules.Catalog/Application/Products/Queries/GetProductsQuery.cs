using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Catalog.Application.Products.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.Products.Queries;

public sealed record GetProductsQuery(
    Guid? CategoryId = null,
    Guid? BrandId = null,
    string? Search = null,
    bool IncludeInactive = false) : IRequest<IReadOnlyList<ProductDto>>;

public sealed class GetProductsHandler(IAppDbContext db)
    : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var query = db.Set<Product>().AsNoTracking();
        if (request.CategoryId is { } cid) query = query.Where(p => p.CategoryId == cid);
        if (request.BrandId is { } bid) query = query.Where(p => p.BrandId == bid);
        if (!request.IncludeInactive) query = query.Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(p => p.Name.Contains(s) || p.Sku.Contains(s));
        }

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(
                p.Id, p.CategoryId, p.UnitId, p.BrandId, p.TaxRateId,
                p.Sku, p.Name, p.Description, p.Barcode,
                p.BasePrice.Amount, p.BasePrice.Currency,
                EF.Property<decimal?>(p, "CostPriceAmount"),
                EF.Property<string?>(p, "CostPriceCurrency"),
                (int)p.Type, p.Type.ToString(),
                p.ImageUrl, p.HsnCode, p.ReorderPoint, p.Weight,
                p.IsTaxable, p.IsSold, p.IsPurchased,
                p.IsActive, p.IsAvailable, p.IsStockTracked))
            .ToListAsync(ct);
    }
}
