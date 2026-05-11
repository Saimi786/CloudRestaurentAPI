using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Inventory.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Branch = CloudRestaurent.Domain.Companies.Branch;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;

namespace CloudRestaurent.Modules.Inventory.Application.Queries;

public sealed record StockValuationRow(
    Guid BranchId, string BranchName,
    Guid ProductId, string Sku, string Name,
    decimal Quantity, decimal? UnitCost, decimal? Value);

public sealed record StockValuationDto(
    decimal TotalValue,
    IReadOnlyList<StockValuationRow> Rows);

public sealed record GetStockValuationQuery(Guid? BranchId) : IRequest<StockValuationDto>;

public sealed class GetStockValuationHandler(IAppDbContext db)
    : IRequestHandler<GetStockValuationQuery, StockValuationDto>
{
    public async Task<StockValuationDto> Handle(GetStockValuationQuery request, CancellationToken ct)
    {
        var balances = db.Set<StockBalance>().AsNoTracking().Where(b => b.Quantity != 0);
        if (request.BranchId.HasValue) balances = balances.Where(b => b.BranchId == request.BranchId.Value);

        var rows = await (
            from b in balances
            join br in db.Set<Branch>().AsNoTracking() on b.BranchId equals br.Id
            join p in db.Set<Product>().AsNoTracking() on b.ProductId equals p.Id
            select new {
                b.BranchId, BranchName = br.Name,
                b.ProductId, p.Sku, p.Name,
                b.Quantity,
                UnitCost = EF.Property<decimal?>(p, "CostPriceAmount")
            })
            .OrderBy(x => x.BranchName).ThenBy(x => x.Name)
            .ToListAsync(ct);

        var dtoRows = rows.Select(r =>
        {
            decimal? value = r.UnitCost.HasValue ? r.Quantity * r.UnitCost.Value : null;
            return new StockValuationRow(
                r.BranchId, r.BranchName, r.ProductId, r.Sku, r.Name,
                r.Quantity, r.UnitCost, value);
        }).ToList();

        var total = dtoRows.Sum(r => r.Value ?? 0);
        return new StockValuationDto(total, dtoRows);
    }
}
