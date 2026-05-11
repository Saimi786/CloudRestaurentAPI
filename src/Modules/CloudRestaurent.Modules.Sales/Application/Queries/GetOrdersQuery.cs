using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Sales.Application.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Contacts.Domain;
using CloudRestaurent.Modules.Restaurant.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Queries;

public sealed record GetOrdersQuery(
    Guid? BranchId = null,
    OrderStatus? Status = null,
    Guid? CustomerId = null,
    int Limit = 100) : IRequest<IReadOnlyList<OrderSummaryDto>>;

public sealed class GetOrdersHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetOrdersQuery, IReadOnlyList<OrderSummaryDto>>
{
    public async Task<IReadOnlyList<OrderSummaryDto>> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        var query = db.Set<Order>().AsNoTracking();
        if (request.BranchId is { } b)
        {
            currentUser.EnsureCanAccess(b);
            query = query.Where(o => o.BranchId == b);
        }
        else if (!currentUser.CanAccessAllBranches)
        {
            // No branch supplied + scoped user → silently intersect with their assignments.
            var allowed = currentUser.BranchIds;
            query = query.Where(o => allowed.Contains(o.BranchId));
        }
        if (request.Status is { } s) query = query.Where(o => o.Status == s);
        if (request.CustomerId is { } c) query = query.Where(o => o.CustomerId == c);

        var limit = Math.Clamp(request.Limit, 1, 500);

        // Order totals are stored on the Order row itself now — no per-line aggregation needed.
        var headers = await (
            from o in query
            join br in db.Set<Branch>().AsNoTracking() on o.BranchId equals br.Id
            join t in db.Set<RestaurantTable>().AsNoTracking() on o.TableId equals t.Id into ts
            from t in ts.DefaultIfEmpty()
            join cust in db.Set<Customer>().AsNoTracking() on o.CustomerId equals cust.Id into cs
            from cust in cs.DefaultIfEmpty()
            orderby o.OpenedAt descending
            select new
            {
                o.Id,
                o.OrderNumber,
                o.BranchId,
                BranchName = br.Name,
                TableCode = t != null ? t.Code : null,
                CustomerName = cust != null ? cust.FullName : null,
                o.Type,
                o.Status,
                o.Currency,
                o.SubtotalAmount,
                o.TaxAmount,
                o.DiscountAmount,
                o.GrandTotalAmount,
                o.OpenedAt,
                o.ClosedAt
            }
        ).Take(limit).ToListAsync(ct);

        if (headers.Count == 0) return Array.Empty<OrderSummaryDto>();

        var orderIds = headers.Select(h => h.Id).ToList();

        var lineCountByOrder = (await db.Set<OrderLine>().AsNoTracking()
                .Where(l => orderIds.Contains(l.OrderId))
                .Select(l => new { l.OrderId })
                .ToListAsync(ct))
            .GroupBy(l => l.OrderId)
            .ToDictionary(g => g.Key, g => g.Count());

        var paidByOrder = (await db.Set<Payment>().AsNoTracking()
                .Where(p => orderIds.Contains(p.OrderId))
                .Select(p => new { p.OrderId, Amount = p.Amount.Amount })
                .ToListAsync(ct))
            .GroupBy(p => p.OrderId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        return headers.Select(h =>
        {
            var paid = paidByOrder.GetValueOrDefault(h.Id);
            return new OrderSummaryDto(
                h.Id, h.OrderNumber,
                h.BranchId, h.BranchName,
                h.TableCode, h.CustomerName,
                h.Type, h.Type.ToString(),
                h.Status, h.Status.ToString(),
                h.Currency,
                lineCountByOrder.GetValueOrDefault(h.Id),
                h.SubtotalAmount, h.TaxAmount, h.DiscountAmount, h.GrandTotalAmount,
                h.GrandTotalAmount - paid,
                h.OpenedAt, h.ClosedAt);
        }).ToList();
    }
}
