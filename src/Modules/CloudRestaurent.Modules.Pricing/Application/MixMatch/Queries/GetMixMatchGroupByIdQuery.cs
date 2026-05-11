using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Domain;
using CloudRestaurent.Modules.Pricing.Application.MixMatch.Dtos;
using CloudRestaurent.Modules.Pricing.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Pricing.Application.MixMatch.Queries;

public sealed record GetMixMatchGroupByIdQuery(Guid Id) : IRequest<MixMatchGroupDetailDto>;

public sealed class GetMixMatchGroupByIdHandler(IAppDbContext db)
    : IRequestHandler<GetMixMatchGroupByIdQuery, MixMatchGroupDetailDto>
{
    public async Task<MixMatchGroupDetailDto> Handle(GetMixMatchGroupByIdQuery request, CancellationToken ct)
    {
        var g = await db.Set<MixMatchGroup>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException("MixMatchGroup", request.Id);

        // Pull attached products with cost / retail / margin for the table.
        var products = await (
            from mp in db.Set<MixMatchProduct>().AsNoTracking()
            where mp.MixMatchGroupId == g.Id
            join p in db.Set<Product>().AsNoTracking() on mp.ProductId equals p.Id
            select new
            {
                mp.Id, mp.ProductId,
                p.Sku, p.Name,
                CostAmount = EF.Property<decimal?>(p, "CostPriceAmount"),
                CostCurrency = EF.Property<string?>(p, "CostPriceCurrency"),
                RetailAmount = p.BasePrice.Amount,
                Currency = p.BasePrice.Currency
            })
            .ToListAsync(ct);

        var productDtos = products.Select(p =>
        {
            decimal? margin = null;
            if (p.CostAmount is { } c && c > 0 && p.RetailAmount > 0)
                margin = Math.Round(((p.RetailAmount - c) / p.RetailAmount) * 100m, 2);
            return new MixMatchProductDto(
                p.Id, p.ProductId, p.Sku, p.Name,
                p.CostAmount, p.RetailAmount, p.Currency, margin);
        }).ToList();

        return new MixMatchGroupDetailDto(
            g.Id, g.Name, g.Type, g.Type.ToString(),
            g.Quantity, g.DiscountValue,
            g.StartDate, g.EndDate, g.DaysOfWeek, g.StartTime, g.EndTime,
            g.Priority, g.Stackable, g.IsActive,
            productDtos);
    }
}
