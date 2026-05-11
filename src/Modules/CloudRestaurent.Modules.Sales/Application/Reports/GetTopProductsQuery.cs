using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Sales.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;

namespace CloudRestaurent.Modules.Sales.Application.Reports;

public sealed record GetTopProductsQuery(
    DateTimeOffset From, DateTimeOffset To, Guid? BranchId, int Take = 20)
    : IRequest<IReadOnlyList<TopProductRow>>;

public sealed class GetTopProductsHandler(IAppDbContext db)
    : IRequestHandler<GetTopProductsQuery, IReadOnlyList<TopProductRow>>
{
    public async Task<IReadOnlyList<TopProductRow>> Handle(GetTopProductsQuery request, CancellationToken ct)
    {
        var orders = db.Set<Order>().AsNoTracking()
            .Where(o => o.Status == OrderStatus.Closed
                     && o.ClosedAt >= request.From
                     && o.ClosedAt < request.To);
        if (request.BranchId.HasValue)
            orders = orders.Where(o => o.BranchId == request.BranchId.Value);

        var orderIds = await orders.Select(o => o.Id).ToListAsync(ct);

        var grouped = await (
            from l in db.Set<OrderLine>().AsNoTracking()
            where orderIds.Contains(l.OrderId)
            group l by l.ProductId into g
            select new {
                ProductId = g.Key,
                Quantity = g.Sum(l => l.Quantity),
                Revenue = g.Sum(l => l.LineSubtotal)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(request.Take)
            .ToListAsync(ct);

        var productIds = grouped.Select(g => g.ProductId).ToList();
        var products = await db.Set<Product>().AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Sku, p.Name })
            .ToDictionaryAsync(p => p.Id, ct);

        return grouped.Select(g =>
        {
            var p = products.GetValueOrDefault(g.ProductId);
            return new TopProductRow(g.ProductId, p?.Sku ?? "—", p?.Name ?? "—", g.Quantity, g.Revenue);
        }).ToList();
    }
}
