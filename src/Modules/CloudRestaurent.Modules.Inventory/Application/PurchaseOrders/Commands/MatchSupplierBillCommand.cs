using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Inventory.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Inventory.Application.PurchaseOrders.Commands;

/// <summary>
/// Compute the expected total against the linked PO (sum of received-qty × PO unit-cost) and
/// flag the bill Matched / Over / Under depending on tolerance. Caller may pass an
/// override reason to keep alongside (e.g. "agreed price increase, accepting variance").
/// </summary>
public sealed record MatchSupplierBillCommand(
    Guid BillId, decimal Tolerance = 0.01m, string? OverrideReason = null) : IRequest<BillMatchStatus>;

public sealed class MatchSupplierBillValidator : AbstractValidator<MatchSupplierBillCommand>
{
    public MatchSupplierBillValidator()
    {
        RuleFor(x => x.BillId).NotEmpty();
        RuleFor(x => x.Tolerance).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OverrideReason).MaximumLength(1000);
    }
}

public sealed class MatchSupplierBillHandler(IAppDbContext db, ICurrentUser user)
    : IRequestHandler<MatchSupplierBillCommand, BillMatchStatus>
{
    public async Task<BillMatchStatus> Handle(MatchSupplierBillCommand request, CancellationToken ct)
    {
        var userId = user.UserId
            ?? throw new UnauthorizedException("No authenticated user.");

        var bill = await db.Set<SupplierBill>().FirstOrDefaultAsync(b => b.Id == request.BillId, ct)
            ?? throw new NotFoundException("SupplierBill", request.BillId);

        if (bill.PurchaseOrderId is not { } poId)
            throw new BusinessRuleException("Cannot 3-way match a bill that isn't linked to a PO.");

        var lines = await db.Set<PurchaseOrderLine>().AsNoTracking()
            .Where(l => l.PurchaseOrderId == poId)
            .Select(l => new { l.ReceivedQuantity, l.UnitCost })
            .ToListAsync(ct);
        if (lines.Count == 0)
            throw new BusinessRuleException("Linked PO has no lines.");

        var expected = lines.Sum(l => Math.Round(l.ReceivedQuantity * l.UnitCost, 4));
        bill.RecordMatch(expected, request.Tolerance, userId, request.OverrideReason);
        await db.SaveChangesAsync(ct);
        return bill.MatchStatus;
    }
}

public sealed record DisputeSupplierBillCommand(Guid BillId, string Reason) : IRequest;

public sealed class DisputeSupplierBillValidator : AbstractValidator<DisputeSupplierBillCommand>
{
    public DisputeSupplierBillValidator()
    {
        RuleFor(x => x.BillId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public sealed class DisputeSupplierBillHandler(IAppDbContext db, ICurrentUser user)
    : IRequestHandler<DisputeSupplierBillCommand>
{
    public async Task Handle(DisputeSupplierBillCommand request, CancellationToken ct)
    {
        var userId = user.UserId
            ?? throw new UnauthorizedException("No authenticated user.");
        var bill = await db.Set<SupplierBill>().FirstOrDefaultAsync(b => b.Id == request.BillId, ct)
            ?? throw new NotFoundException("SupplierBill", request.BillId);
        bill.FlagDisputed(userId, request.Reason);
        await db.SaveChangesAsync(ct);
    }
}

public sealed record UpdateSupplierBillCommand(
    Guid Id,
    decimal Amount,
    string? SupplierBillReference,
    DateOnly BillDate,
    DateOnly? DueDate,
    string? Notes) : IRequest;

public sealed class UpdateSupplierBillValidator : AbstractValidator<UpdateSupplierBillCommand>
{
    public UpdateSupplierBillValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.SupplierBillReference).MaximumLength(60);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class UpdateSupplierBillHandler(IAppDbContext db) : IRequestHandler<UpdateSupplierBillCommand>
{
    public async Task Handle(UpdateSupplierBillCommand request, CancellationToken ct)
    {
        var bill = await db.Set<SupplierBill>().FirstOrDefaultAsync(b => b.Id == request.Id, ct)
            ?? throw new NotFoundException("SupplierBill", request.Id);
        bill.UpdateBillDetails(request.Amount, request.SupplierBillReference,
            request.BillDate, request.DueDate, request.Notes);
        await db.SaveChangesAsync(ct);
    }
}
