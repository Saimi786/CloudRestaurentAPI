using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Products.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.Products.Queries;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;

public sealed class GetProductByIdHandler(IAppDbContext db) : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        var dto = await db.Set<Product>().AsNoTracking()
            .Where(p => p.Id == request.Id)
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
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Product", request.Id);
        return dto;
    }
}
