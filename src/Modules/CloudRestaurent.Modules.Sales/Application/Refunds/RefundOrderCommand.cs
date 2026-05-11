using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Inventory.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Refunds;

public sealed record RefundOrderCommand(
    Guid OrderId,
    decimal Amount,
    PaymentMethod Method,
    string? Reason,
    IReadOnlyList<RefundLineInput>? Lines) : IRequest<RefundDto>;

public sealed class RefundOrderValidator : AbstractValidator<RefundOrderCommand>
{
    public RefundOrderValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public sealed class RefundOrderHandler(
    IAppDbContext db,
    ITenantContext tenant,
    ICurrentUser user,
    ILedgerPoster ledger,
    IIdentityService identity)
    : IRequestHandler<RefundOrderCommand, RefundDto>
{
    public async Task<RefundDto> Handle(RefundOrderCommand request, CancellationToken ct)
    {
        var tenantId = tenant.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");
        var userId = user.UserId
            ?? throw new UnauthorizedException("No authenticated user.");

        var order = await db.Set<Order>().Include(o => o.Lines).Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (order.Status != OrderStatus.Closed)
            throw new BusinessRuleException("Only closed orders can be refunded.");

        var alreadyRefunded = await db.Set<Refund>()
            .Where(r => r.OrderId == order.Id).SumAsync(r => (decimal?)r.Amount, ct) ?? 0;
        if (alreadyRefunded + request.Amount - order.GrandTotalAmount > 0.01m)
            throw new BusinessRuleException(
                $"Refund of {request.Amount:0.00} would exceed already-refunded {alreadyRefunded:0.00} of total {order.GrandTotalAmount:0.00}.");

        var refund = new Refund(
            Guid.NewGuid(), tenantId, order.Id, order.BranchId, userId,
            request.Amount, order.Currency, request.Method, request.Reason);

        // Optional return-lines: create stock-back movements for restock=true lines.
        if (request.Lines is { Count: > 0 })
        {
            // Pull the canonical product unit for any line we restock — needed for StockMovement.
            var productIds = request.Lines.Select(rl =>
                order.Lines.First(l => l.Id == rl.OrderLineId).ProductId).Distinct().ToList();
            var productUnits = await db.Set<CloudRestaurent.Modules.Catalog.Domain.Product>().AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.UnitId })
                .ToDictionaryAsync(p => p.Id, p => p.UnitId, ct);

            foreach (var rl in request.Lines)
            {
                var orderLine = order.Lines.FirstOrDefault(l => l.Id == rl.OrderLineId)
                    ?? throw new BusinessRuleException($"OrderLine {rl.OrderLineId} not on this order.");
                if (rl.Quantity <= 0 || rl.Quantity > orderLine.Quantity)
                    throw new BusinessRuleException("Return qty must be > 0 and ≤ original line qty.");

                refund.AddLine(orderLine.Id, orderLine.ProductId, rl.Quantity, rl.Restock);

                if (rl.Restock && productUnits.TryGetValue(orderLine.ProductId, out var unitId))
                {
                    var occurredAt = DateTimeOffset.UtcNow;
                    db.Set<StockMovement>().Add(new StockMovement(
                        Guid.NewGuid(), tenantId, order.BranchId, orderLine.ProductId,
                        unitId, StockMovementType.TransferIn,
                        rl.Quantity, rl.Quantity,
                        $"Refund {order.OrderNumber}", $"Restocked from refund {refund.Id:N}",
                        occurredAt));

                    var balance = await db.Set<StockBalance>()
                        .FirstOrDefaultAsync(b =>
                            b.BranchId == order.BranchId && b.ProductId == orderLine.ProductId, ct);
                    if (balance is null)
                    {
                        balance = new StockBalance(Guid.NewGuid(), tenantId, order.BranchId, orderLine.ProductId);
                        db.Set<StockBalance>().Add(balance);
                    }
                    balance.Apply(rl.Quantity, occurredAt);
                }
            }
        }

        db.Set<Refund>().Add(refund);

        // Cash refund out of an open till → register-side Refund movement
        if (request.Method == PaymentMethod.Cash)
        {
            var shift = await db.Set<CashRegisterShift>()
                .FirstOrDefaultAsync(s =>
                    s.Status == ShiftStatus.Open &&
                    s.OpenedByUserId == userId &&
                    s.BranchId == order.BranchId, ct);
            shift?.RecordMovement(ShiftMovementType.Refund, request.Amount, refund.Id,
                order.OrderNumber, request.Reason);
        }

        await db.SaveChangesAsync(ct);
        await ledger.PostRefundAsync(tenantId, refund.Id, ct);

        var users = await identity.ListUsersAsync(tenantId, includeInactive: true, ct);
        var byId = users.ToDictionary(u => u.Id, u => u.FullName);

        var lineDtos = await BuildLineDtos(db, refund.Id, ct);
        return new RefundDto(
            refund.Id, refund.OrderId, order.OrderNumber, refund.BranchId,
            refund.RefundedByUserId, byId.GetValueOrDefault(refund.RefundedByUserId, "—"),
            refund.Amount, refund.Currency,
            refund.Method, refund.Method.ToString(),
            refund.Reason, refund.RefundedAt, lineDtos);
    }

    internal static async Task<IReadOnlyList<RefundLineDto>> BuildLineDtos(
        IAppDbContext db, Guid refundId, CancellationToken ct)
    {
        var rows = await (
            from rl in db.Set<RefundLine>().AsNoTracking()
            where rl.RefundId == refundId
            join p in db.Set<CloudRestaurent.Modules.Catalog.Domain.Product>().AsNoTracking()
                on rl.ProductId equals p.Id into pj
            from p in pj.DefaultIfEmpty()
            select new { rl.Id, rl.OrderLineId, rl.ProductId, ProductName = p != null ? p.Name : "—", rl.Quantity, rl.Restock })
            .ToListAsync(ct);
        return rows.Select(r => new RefundLineDto(
            r.Id, r.OrderLineId, r.ProductId, r.ProductName, r.Quantity, r.Restock)).ToList();
    }
}
