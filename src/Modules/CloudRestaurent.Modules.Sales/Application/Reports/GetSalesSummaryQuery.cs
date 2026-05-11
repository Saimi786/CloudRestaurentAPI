using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Sales.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Reports;

public sealed record GetSalesSummaryQuery(
    DateTimeOffset From, DateTimeOffset To, Guid? BranchId)
    : IRequest<SalesSummaryDto>;

public sealed class GetSalesSummaryHandler(IAppDbContext db)
    : IRequestHandler<GetSalesSummaryQuery, SalesSummaryDto>
{
    public async Task<SalesSummaryDto> Handle(GetSalesSummaryQuery request, CancellationToken ct)
    {
        var orders = db.Set<Order>().AsNoTracking()
            .Where(o => o.Status == OrderStatus.Closed
                     && o.ClosedAt >= request.From
                     && o.ClosedAt < request.To);
        if (request.BranchId.HasValue)
            orders = orders.Where(o => o.BranchId == request.BranchId.Value);

        var rows = await orders
            .Select(o => new {
                Day = o.ClosedAt!.Value.UtcDateTime.Date,
                o.SubtotalAmount, o.TaxAmount, o.DiscountAmount, o.GrandTotalAmount
            })
            .ToListAsync(ct);

        var byDay = rows
            .GroupBy(r => r.Day)
            .OrderBy(g => g.Key)
            .Select(g => new SalesByDayRow(
                DateOnly.FromDateTime(g.Key),
                g.Count(),
                g.Sum(x => x.SubtotalAmount),
                g.Sum(x => x.TaxAmount),
                g.Sum(x => x.DiscountAmount),
                g.Sum(x => x.GrandTotalAmount)))
            .ToList();

        var refunds = db.Set<Refund>().AsNoTracking()
            .Where(r => r.RefundedAt >= request.From && r.RefundedAt < request.To);
        if (request.BranchId.HasValue) refunds = refunds.Where(r => r.BranchId == request.BranchId.Value);
        var refundTotal = await refunds.SumAsync(r => (decimal?)r.Amount, ct) ?? 0m;

        var subtotal = rows.Sum(r => r.SubtotalAmount);
        var tax = rows.Sum(r => r.TaxAmount);
        var discount = rows.Sum(r => r.DiscountAmount);
        var grand = rows.Sum(r => r.GrandTotalAmount);

        return new SalesSummaryDto(
            request.From, request.To,
            rows.Count, subtotal, tax, discount, grand, refundTotal,
            grand - refundTotal,
            byDay);
    }
}
