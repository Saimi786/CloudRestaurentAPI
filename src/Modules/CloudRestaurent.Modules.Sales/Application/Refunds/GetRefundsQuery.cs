using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Sales.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Refunds;

public sealed record GetRefundsForOrderQuery(Guid OrderId) : IRequest<IReadOnlyList<RefundDto>>;

public sealed class GetRefundsForOrderHandler(IAppDbContext db, IIdentityService identity, ITenantContext tenant)
    : IRequestHandler<GetRefundsForOrderQuery, IReadOnlyList<RefundDto>>
{
    public async Task<IReadOnlyList<RefundDto>> Handle(GetRefundsForOrderQuery request, CancellationToken ct)
    {
        var refunds = await db.Set<Refund>().AsNoTracking()
            .Where(r => r.OrderId == request.OrderId)
            .OrderByDescending(r => r.RefundedAt)
            .ToListAsync(ct);
        if (refunds.Count == 0) return Array.Empty<RefundDto>();

        var order = await db.Set<Order>().AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);

        var users = tenant.TenantId is { } tid
            ? (await identity.ListUsersAsync(tid, true, ct)).ToDictionary(u => u.Id, u => u.FullName)
            : new();

        var result = new List<RefundDto>();
        foreach (var r in refunds)
        {
            var lines = await RefundOrderHandler.BuildLineDtos(db, r.Id, ct);
            result.Add(new RefundDto(
                r.Id, r.OrderId, order?.OrderNumber, r.BranchId,
                r.RefundedByUserId, users.GetValueOrDefault(r.RefundedByUserId, "—"),
                r.Amount, r.Currency,
                r.Method, r.Method.ToString(),
                r.Reason, r.RefundedAt, lines));
        }
        return result;
    }
}
