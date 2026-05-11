using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Inventory.Application.Commands;
using CloudRestaurent.Modules.Inventory.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Branch = CloudRestaurent.Domain.Companies.Branch;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Inventory.Application.Queries;

public sealed record GetWasteLogQuery(
    Guid? BranchId, WasteReason? Reason,
    DateTimeOffset? From, DateTimeOffset? To, int Take = 200)
    : IRequest<IReadOnlyList<WasteLogDto>>;

public sealed class GetWasteLogHandler(IAppDbContext db) : IRequestHandler<GetWasteLogQuery, IReadOnlyList<WasteLogDto>>
{
    public async Task<IReadOnlyList<WasteLogDto>> Handle(GetWasteLogQuery request, CancellationToken ct)
    {
        var q = db.Set<WasteLog>().AsNoTracking();
        if (request.BranchId.HasValue) q = q.Where(w => w.BranchId == request.BranchId.Value);
        if (request.Reason.HasValue) q = q.Where(w => w.Reason == request.Reason.Value);
        if (request.From.HasValue) q = q.Where(w => w.OccurredAt >= request.From.Value);
        if (request.To.HasValue) q = q.Where(w => w.OccurredAt < request.To.Value);

        var rows = await (
            from w in q.OrderByDescending(w => w.OccurredAt).Take(request.Take)
            join b in db.Set<Branch>().AsNoTracking() on w.BranchId equals b.Id
            join p in db.Set<Product>().AsNoTracking() on w.ProductId equals p.Id
            join u in db.Set<Unit>().AsNoTracking() on w.UnitId equals u.Id
            join pu in db.Set<Unit>().AsNoTracking() on p.UnitId equals pu.Id
            select new
            {
                w.Id, w.BranchId, BranchName = b.Name,
                w.ProductId, p.Sku, ProductName = p.Name,
                w.UnitId, UnitCode = u.Code,
                w.Quantity, w.QuantityInProductUnit, ProductUnitCode = pu.Code,
                w.Reason, w.Notes, w.CreatedByUserId, w.OccurredAt
            }).ToListAsync(ct);

        return rows.Select(r => new WasteLogDto(
            r.Id, r.BranchId, r.BranchName,
            r.ProductId, r.Sku, r.ProductName,
            r.UnitId, r.UnitCode,
            r.Quantity, r.QuantityInProductUnit, r.ProductUnitCode,
            r.Reason, r.Reason.ToString(),
            r.Notes,
            r.CreatedByUserId, r.OccurredAt)).ToList();
    }
}
