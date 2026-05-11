using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Contacts.Domain;
using CloudRestaurent.Modules.Inventory.Application.PurchaseOrders.Dtos;
using CloudRestaurent.Modules.Inventory.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Branch = CloudRestaurent.Domain.Companies.Branch;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Inventory.Application.PurchaseOrders.Queries;

public sealed record GetPurchaseOrdersQuery(
    Guid? BranchId, Guid? SupplierId, PurchaseOrderStatus? Status, int Take = 200)
    : IRequest<IReadOnlyList<PurchaseOrderSummaryDto>>;

public sealed class GetPurchaseOrdersHandler(IAppDbContext db)
    : IRequestHandler<GetPurchaseOrdersQuery, IReadOnlyList<PurchaseOrderSummaryDto>>
{
    public async Task<IReadOnlyList<PurchaseOrderSummaryDto>> Handle(GetPurchaseOrdersQuery request, CancellationToken ct)
    {
        var q = db.Set<PurchaseOrder>().AsNoTracking();
        if (request.BranchId.HasValue) q = q.Where(p => p.BranchId == request.BranchId.Value);
        if (request.SupplierId.HasValue) q = q.Where(p => p.SupplierId == request.SupplierId.Value);
        if (request.Status.HasValue) q = q.Where(p => p.Status == request.Status.Value);

        var rows = await (
            from po in q.OrderByDescending(p => p.CreatedAt).Take(request.Take)
            join s in db.Set<Customer>().AsNoTracking() on po.SupplierId equals s.Id
            select new
            {
                po.Id, po.Number, SupplierName = s.SupplierBusinessName ?? s.FullName,
                po.Status, po.ExpectedDate,
                po.GrandTotalAmount, po.Currency, po.CreatedAt
            }).ToListAsync(ct);

        return rows.Select(r => new PurchaseOrderSummaryDto(
            r.Id, r.Number, r.SupplierName,
            r.Status, r.Status.ToString(), r.ExpectedDate,
            r.GrandTotalAmount, r.Currency, r.CreatedAt)).ToList();
    }
}

public sealed record GetPurchaseOrderByIdQuery(Guid Id) : IRequest<PurchaseOrderDto>;

public sealed class GetPurchaseOrderByIdHandler(IAppDbContext db)
    : IRequestHandler<GetPurchaseOrderByIdQuery, PurchaseOrderDto>
{
    public async Task<PurchaseOrderDto> Handle(GetPurchaseOrderByIdQuery request, CancellationToken ct)
    {
        var po = await db.Set<PurchaseOrder>().AsNoTracking()
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new NotFoundException("PurchaseOrder", request.Id);

        var branch = await db.Set<Branch>().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == po.BranchId, ct);
        var supplier = await db.Set<Customer>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == po.SupplierId, ct);

        var unitIds = po.Lines.Select(l => l.UnitId).Distinct().ToList();
        var units = await db.Set<Unit>().AsNoTracking()
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var lineDtos = po.Lines.Select(l => new PurchaseOrderLineDto(
            l.Id, l.ProductId, l.ProductSku, l.ProductName,
            l.UnitId, units.GetValueOrDefault(l.UnitId)?.Code ?? "—",
            l.OrderedQuantity, l.ReceivedQuantity,
            l.UnitCost, l.LineTotal, l.Notes)).ToList();

        return new PurchaseOrderDto(
            po.Id, po.BranchId, branch?.Name ?? "—",
            po.SupplierId, supplier?.SupplierBusinessName ?? supplier?.FullName ?? "—",
            po.Number, po.Status, po.Status.ToString(),
            po.ExpectedDate, po.Currency, po.Notes,
            po.SubtotalAmount, po.TaxAmount, po.GrandTotalAmount,
            po.CreatedAt, po.SentAt, po.ClosedAt,
            lineDtos);
    }
}
