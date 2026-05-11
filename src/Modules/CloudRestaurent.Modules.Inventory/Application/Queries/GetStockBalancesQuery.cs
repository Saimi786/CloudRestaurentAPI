using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Inventory.Application.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Inventory.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Inventory.Application.Queries;

public sealed record GetStockBalancesQuery(
    Guid? BranchId = null,
    Guid? ProductId = null,
    string? Search = null) : IRequest<IReadOnlyList<StockBalanceDto>>;

public sealed class GetStockBalancesHandler(IAppDbContext db)
    : IRequestHandler<GetStockBalancesQuery, IReadOnlyList<StockBalanceDto>>
{
    public async Task<IReadOnlyList<StockBalanceDto>> Handle(GetStockBalancesQuery request, CancellationToken ct)
    {
        var balances = db.Set<StockBalance>().AsNoTracking();
        if (request.BranchId is { } bid) balances = balances.Where(b => b.BranchId == bid);
        if (request.ProductId is { } pid) balances = balances.Where(b => b.ProductId == pid);

        var query =
            from b in balances
            join br in db.Set<Branch>().AsNoTracking() on b.BranchId equals br.Id
            join p in db.Set<Product>().AsNoTracking() on b.ProductId equals p.Id
            join u in db.Set<Unit>().AsNoTracking() on p.UnitId equals u.Id
            select new { b, br, p, u };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(x => x.p.Name.Contains(s) || x.p.Sku.Contains(s));
        }

        return await query
            .OrderBy(x => x.br.Name).ThenBy(x => x.p.Name)
            .Select(x => new StockBalanceDto(
                x.b.Id, x.b.BranchId, x.br.Name,
                x.p.Id, x.p.Sku, x.p.Name, x.u.Code,
                x.b.Quantity, x.b.LastMovementAt))
            .ToListAsync(ct);
    }
}
