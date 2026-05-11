using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Inventory.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Inventory.Application.PurchaseOrders.Commands;

public sealed record PaySupplierBillCommand(
    Guid BillId, decimal Amount, SupplierBillPaymentMethod Method, string? Reference) : IRequest<Guid>;

public sealed class PaySupplierBillValidator : AbstractValidator<PaySupplierBillCommand>
{
    public PaySupplierBillValidator()
    {
        RuleFor(x => x.BillId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Reference).MaximumLength(120);
    }
}

public sealed class PaySupplierBillHandler(
    IAppDbContext db, ICurrentUser user, ILedgerPoster ledger, ITenantContext tenant)
    : IRequestHandler<PaySupplierBillCommand, Guid>
{
    public async Task<Guid> Handle(PaySupplierBillCommand request, CancellationToken ct)
    {
        var tenantId = tenant.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");
        var userId = user.UserId
            ?? throw new UnauthorizedException("No authenticated user.");

        var bill = await db.Set<SupplierBill>().Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == request.BillId, ct)
            ?? throw new NotFoundException("SupplierBill", request.BillId);

        if (request.Amount > bill.Outstanding() + 0.0001m)
            throw new BusinessRuleException(
                $"Payment {request.Amount} exceeds outstanding {bill.Outstanding():0.00}.");

        var payment = new SupplierBillPayment(
            Guid.NewGuid(), bill.Id, request.Amount, request.Method, request.Reference,
            userId, DateTimeOffset.UtcNow);
        bill.AddPayment(payment);
        db.Set<SupplierBillPayment>().Add(payment);

        await db.SaveChangesAsync(ct);
        await ledger.PostSupplierBillPaymentAsync(tenantId, payment.Id, ct);

        return payment.Id;
    }
}

public sealed record GetSupplierBillsQuery(
    Guid? SupplierId, SupplierBillStatus? Status, int Take = 200)
    : IRequest<IReadOnlyList<SupplierBillDto>>;

public sealed record SupplierBillDto(
    Guid Id,
    Guid SupplierId,
    string SupplierName,
    Guid? PurchaseOrderId,
    string? PurchaseOrderNumber,
    string Number,
    string? SupplierBillReference,
    DateOnly BillDate,
    DateOnly? DueDate,
    decimal Amount,
    decimal PaidAmount,
    decimal Outstanding,
    string Currency,
    SupplierBillStatus Status,
    string StatusName,
    BillMatchStatus MatchStatus,
    string MatchStatusName,
    decimal? ExpectedAmount,
    decimal? DiscrepancyAmount,
    string? DiscrepancyReason,
    DateTimeOffset? MatchedAt,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed class GetSupplierBillsHandler(IAppDbContext db)
    : IRequestHandler<GetSupplierBillsQuery, IReadOnlyList<SupplierBillDto>>
{
    public async Task<IReadOnlyList<SupplierBillDto>> Handle(GetSupplierBillsQuery request, CancellationToken ct)
    {
        var q = db.Set<SupplierBill>().AsNoTracking();
        if (request.SupplierId.HasValue) q = q.Where(b => b.SupplierId == request.SupplierId.Value);
        if (request.Status.HasValue) q = q.Where(b => b.Status == request.Status.Value);

        var rows = await (
            from b in q.OrderByDescending(b => b.CreatedAt).Take(request.Take)
            join s in db.Set<CloudRestaurent.Modules.Contacts.Domain.Customer>().AsNoTracking()
                on b.SupplierId equals s.Id
            join po in db.Set<PurchaseOrder>().AsNoTracking() on b.PurchaseOrderId equals po.Id into pj
            from po in pj.DefaultIfEmpty()
            select new
            {
                b.Id, b.SupplierId, SupplierName = s.SupplierBusinessName ?? s.FullName,
                b.PurchaseOrderId, PoNumber = po != null ? po.Number : null,
                b.Number, b.SupplierBillReference, b.BillDate, b.DueDate,
                b.Amount, b.PaidAmount, b.Currency, b.Status, b.Notes, b.CreatedAt,
                b.MatchStatus, b.ExpectedAmount, b.DiscrepancyAmount, b.DiscrepancyReason, b.MatchedAt
            }).ToListAsync(ct);

        return rows.Select(r => new SupplierBillDto(
            r.Id, r.SupplierId, r.SupplierName,
            r.PurchaseOrderId, r.PoNumber,
            r.Number, r.SupplierBillReference, r.BillDate, r.DueDate,
            r.Amount, r.PaidAmount, Math.Max(0, r.Amount - r.PaidAmount), r.Currency,
            r.Status, r.Status.ToString(),
            r.MatchStatus, r.MatchStatus.ToString(),
            r.ExpectedAmount, r.DiscrepancyAmount, r.DiscrepancyReason, r.MatchedAt,
            r.Notes, r.CreatedAt)).ToList();
    }
}
