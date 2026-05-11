using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Pricing.Application.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Pricing.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;

namespace CloudRestaurent.Modules.Pricing.Application.Queries;

public sealed record GetPriceRulesQuery(
    Guid? ProductId = null,
    Guid? BranchId = null,
    bool IncludeInactive = false) : IRequest<IReadOnlyList<PriceRuleDto>>;

public sealed class GetPriceRulesHandler(IAppDbContext db)
    : IRequestHandler<GetPriceRulesQuery, IReadOnlyList<PriceRuleDto>>
{
    public async Task<IReadOnlyList<PriceRuleDto>> Handle(GetPriceRulesQuery request, CancellationToken ct)
    {
        var rules = db.Set<PriceRule>().AsNoTracking();
        if (request.ProductId is { } pid) rules = rules.Where(r => r.ProductId == pid);
        if (request.BranchId is { } bid) rules = rules.Where(r => r.BranchId == bid || r.BranchId == null);
        if (!request.IncludeInactive) rules = rules.Where(r => r.IsActive);

        return await (
            from r in rules
            join p in db.Set<Product>().AsNoTracking() on r.ProductId equals p.Id
            join br in db.Set<Branch>().AsNoTracking() on r.BranchId equals br.Id into brs
            from br in brs.DefaultIfEmpty()
            orderby p.Name, r.Priority descending
            select new PriceRuleDto(
                r.Id, r.ProductId, p.Sku, p.Name,
                r.BranchId, br != null ? br.Name : null,
                r.Name, r.StartTime, r.EndTime, r.DaysOfWeek,
                r.OverridePrice.Amount, r.OverridePrice.Currency,
                r.Priority, r.IsActive)
        ).ToListAsync(ct);
    }
}
