using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Inventory.Application.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Inventory.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Inventory.Application.Queries;

public sealed record GetStockMovementsQuery(
    Guid? BranchId = null,
    Guid? ProductId = null,
    StockMovementType? Type = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Limit = 200) : IRequest<IReadOnlyList<StockMovementDto>>;

public sealed class GetStockMovementsHandler(IAppDbContext db)
    : IRequestHandler<GetStockMovementsQuery, IReadOnlyList<StockMovementDto>>
{
    public async Task<IReadOnlyList<StockMovementDto>> Handle(GetStockMovementsQuery request, CancellationToken ct)
    {
        var movements = db.Set<StockMovement>().AsNoTracking();
        if (request.BranchId is { } bid) movements = movements.Where(m => m.BranchId == bid);
        if (request.ProductId is { } pid) movements = movements.Where(m => m.ProductId == pid);
        if (request.Type is { } t) movements = movements.Where(m => m.Type == t);
        if (request.From is { } from) movements = movements.Where(m => m.OccurredAt >= from);
        if (request.To is { } to) movements = movements.Where(m => m.OccurredAt <= to);

        var limit = Math.Clamp(request.Limit, 1, 1000);

        return await (
            from m in movements
            join br in db.Set<Branch>().AsNoTracking() on m.BranchId equals br.Id
            join p in db.Set<Product>().AsNoTracking() on m.ProductId equals p.Id
            join u in db.Set<Unit>().AsNoTracking() on m.UnitId equals u.Id
            join pu in db.Set<Unit>().AsNoTracking() on p.UnitId equals pu.Id
            orderby m.OccurredAt descending
            select new StockMovementDto(
                m.Id, m.BranchId, br.Name,
                m.ProductId, p.Sku, p.Name,
                m.Type, m.Type.ToString(),
                m.UnitId, u.Code,
                m.Quantity, m.QuantityInProductUnit, pu.Code,
                m.Reference, m.Notes, m.OccurredAt, m.CreatedBy)
        ).Take(limit).ToListAsync(ct);
    }
}
