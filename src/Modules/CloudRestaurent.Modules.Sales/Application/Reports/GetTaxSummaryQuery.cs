using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Sales.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Reports;

public sealed record TaxRateRow(
    decimal RatePercentage,
    decimal TaxableSales,    // sum of line subtotals at this rate
    decimal TaxCollected);   // sum of line tax amounts at this rate

public sealed record TaxSummaryDto(
    DateTimeOffset From,
    DateTimeOffset To,
    int OrderCount,
    decimal TotalTaxableSales,
    decimal TotalTaxCollected,
    IReadOnlyList<TaxRateRow> ByRate);

/// <summary>
/// Tax Report — groups closed-order line items by their snapshotted tax rate, so
/// the tax authority breakdown matches what was actually charged on each receipt
/// (not what the current TaxRate row says — those can change after the fact).
/// </summary>
public sealed record GetTaxSummaryQuery(
    DateTimeOffset From, DateTimeOffset To, Guid? BranchId)
    : IRequest<TaxSummaryDto>;

public sealed class GetTaxSummaryHandler(IAppDbContext db)
    : IRequestHandler<GetTaxSummaryQuery, TaxSummaryDto>
{
    public async Task<TaxSummaryDto> Handle(GetTaxSummaryQuery request, CancellationToken ct)
    {
        // Closed orders only — open/voided orders haven't realized tax yet.
        var orderQuery = db.Set<Order>().AsNoTracking()
            .Where(o => o.Status == OrderStatus.Closed
                     && o.ClosedAt >= request.From
                     && o.ClosedAt < request.To);
        if (request.BranchId.HasValue)
            orderQuery = orderQuery.Where(o => o.BranchId == request.BranchId.Value);

        var orderIds = await orderQuery.Select(o => o.Id).ToListAsync(ct);
        if (orderIds.Count == 0)
        {
            return new TaxSummaryDto(request.From, request.To, 0, 0m, 0m, []);
        }

        var lines = await db.Set<OrderLine>().AsNoTracking()
            .Where(l => orderIds.Contains(l.OrderId))
            .Select(l => new { l.TaxRatePercentage, l.LineSubtotal, l.TaxAmount })
            .ToListAsync(ct);

        var byRate = lines
            .GroupBy(l => l.TaxRatePercentage)
            .OrderBy(g => g.Key)
            .Select(g => new TaxRateRow(
                g.Key,
                g.Sum(x => x.LineSubtotal),
                g.Sum(x => x.TaxAmount)))
            .ToList();

        return new TaxSummaryDto(
            request.From, request.To,
            orderIds.Count,
            byRate.Sum(r => r.TaxableSales),
            byRate.Sum(r => r.TaxCollected),
            byRate);
    }
}
