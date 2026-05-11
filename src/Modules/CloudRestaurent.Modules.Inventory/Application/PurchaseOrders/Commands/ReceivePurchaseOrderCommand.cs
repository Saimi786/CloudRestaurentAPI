using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Inventory.Application.PurchaseOrders.Dtos;
using CloudRestaurent.Modules.Inventory.Application.PurchaseOrders.Queries;
using CloudRestaurent.Modules.Inventory.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Inventory.Application.PurchaseOrders.Commands;

/// <summary>
/// GRN — receive (some or all of) a PO. Each line tells us how much of the ordered qty
/// just walked in. Creates StockMovement(Purchase) per line, advances the PO state, posts
/// GL inventory/AP entries, and (on full receive) auto-creates a SupplierBill.
/// </summary>
public sealed record ReceivePurchaseOrderCommand(
    Guid PurchaseOrderId,
    string? SupplierBillReference,   // their invoice #, if available now
    DateOnly? BillDate,
    DateOnly? DueDate,
    IReadOnlyList<ReceiveLineInput> Lines)
    : IRequest<PurchaseOrderDto>;

public sealed class ReceivePurchaseOrderValidator : AbstractValidator<ReceivePurchaseOrderCommand>
{
    public ReceivePurchaseOrderValidator()
    {
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.SupplierBillReference).MaximumLength(60);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.LineId).NotEmpty();
            l.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}

public sealed class ReceivePurchaseOrderHandler(
    IAppDbContext db, ITenantContext tenant, ILedgerPoster ledger, IMediator mediator)
    : IRequestHandler<ReceivePurchaseOrderCommand, PurchaseOrderDto>
{
    public async Task<PurchaseOrderDto> Handle(ReceivePurchaseOrderCommand request, CancellationToken ct)
    {
        var tenantId = tenant.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var po = await db.Set<PurchaseOrder>().Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId, ct)
            ?? throw new NotFoundException("PurchaseOrder", request.PurchaseOrderId);

        var unitIds = po.Lines.Select(l => l.UnitId).Distinct().ToList();
        var productIds = po.Lines.Select(l => l.ProductId).Distinct().ToList();
        var productUnitIds = await db.Set<CloudRestaurent.Modules.Catalog.Domain.Product>().AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.UnitId })
            .ToDictionaryAsync(p => p.Id, p => p.UnitId, ct);
        var allUnitIds = unitIds.Concat(productUnitIds.Values).Distinct().ToList();
        var unitsById = await db.Set<Unit>().AsNoTracking()
            .Where(u => allUnitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var grnBatchId = Guid.NewGuid();      // for GL idempotency
        var occurredAt = DateTimeOffset.UtcNow;
        decimal grnValue = 0m;

        foreach (var input in request.Lines)
        {
            var line = po.Lines.FirstOrDefault(l => l.Id == input.LineId)
                ?? throw new BusinessRuleException($"Line {input.LineId} not on this PO.");

            line.RecordReceipt(input.Quantity);

            // Convert receipt qty to product unit for the StockBalance update.
            if (!unitsById.TryGetValue(line.UnitId, out var lineUnit) ||
                !productUnitIds.TryGetValue(line.ProductId, out var prodUnitId) ||
                !unitsById.TryGetValue(prodUnitId, out var prodUnit))
                throw new BusinessRuleException($"Unit info missing for line {line.Id}.");

            var qtyInProductUnit = input.Quantity * lineUnit.ConversionFactor / prodUnit.ConversionFactor;

            db.Set<StockMovement>().Add(new StockMovement(
                Guid.NewGuid(), tenantId, po.BranchId, line.ProductId, line.UnitId,
                StockMovementType.Purchase, input.Quantity, qtyInProductUnit,
                reference: po.Number,
                notes: $"GRN against {po.Number}",
                occurredAt));

            var balance = await db.Set<StockBalance>()
                .FirstOrDefaultAsync(b => b.BranchId == po.BranchId && b.ProductId == line.ProductId, ct);
            if (balance is null)
            {
                balance = new StockBalance(Guid.NewGuid(), tenantId, po.BranchId, line.ProductId);
                db.Set<StockBalance>().Add(balance);
            }
            balance.Apply(qtyInProductUnit, occurredAt);

            grnValue += Math.Round(input.Quantity * line.UnitCost, 4);
        }

        po.ApplyReceived();
        await db.SaveChangesAsync(ct);

        // GL: debit Inventory, credit AP for the GRN value
        if (grnValue > 0)
        {
            await ledger.PostGoodsReceiptAsync(
                tenantId, po.Id, grnBatchId, grnValue, po.Currency, occurredAt, ct);
        }

        // On full close, create the supplier bill (one bill per fully-received PO).
        if (po.Status == PurchaseOrderStatus.Closed)
        {
            var existing = await db.Set<SupplierBill>().AnyAsync(b => b.PurchaseOrderId == po.Id, ct);
            if (!existing)
            {
                var seq = await db.Set<SupplierBill>().CountAsync(ct) + 1;
                var bill = new SupplierBill(
                    Guid.NewGuid(), tenantId, po.SupplierId, po.Id, po.BranchId,
                    $"BILL-{seq:D5}", request.SupplierBillReference,
                    request.BillDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                    request.DueDate,
                    po.GrandTotalAmount, po.Currency, null);

                // Auto-match: bill amount === PO grand total === sum(received × cost), so this
                // typically lands as Matched. If the supplier later issues a different invoice
                // the user edits the bill amount and re-runs match.
                var expected = po.Lines.Sum(l => Math.Round(l.ReceivedQuantity * l.UnitCost, 4));
                bill.RecordMatch(expected, tolerance: 0.01m, userId: Guid.Empty, overrideReason: null);

                db.Set<SupplierBill>().Add(bill);
                await db.SaveChangesAsync(ct);
            }
        }

        return await mediator.Send(new GetPurchaseOrderByIdQuery(po.Id), ct);
    }
}
