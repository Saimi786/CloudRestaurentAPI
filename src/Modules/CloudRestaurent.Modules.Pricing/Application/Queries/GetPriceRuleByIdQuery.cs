using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Pricing.Application.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Pricing.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;

namespace CloudRestaurent.Modules.Pricing.Application.Queries;

public sealed record GetPriceRuleByIdQuery(Guid Id) : IRequest<PriceRuleDto>;

public sealed class GetPriceRuleByIdHandler(IAppDbContext db)
    : IRequestHandler<GetPriceRuleByIdQuery, PriceRuleDto>
{
    public async Task<PriceRuleDto> Handle(GetPriceRuleByIdQuery request, CancellationToken ct)
    {
        var dto = await (
            from r in db.Set<PriceRule>().AsNoTracking()
            join p in db.Set<Product>().AsNoTracking() on r.ProductId equals p.Id
            join br in db.Set<Branch>().AsNoTracking() on r.BranchId equals br.Id into brs
            from br in brs.DefaultIfEmpty()
            where r.Id == request.Id
            select new PriceRuleDto(
                r.Id, r.ProductId, p.Sku, p.Name,
                r.BranchId, br != null ? br.Name : null,
                r.Name, r.StartTime, r.EndTime, r.DaysOfWeek,
                r.OverridePrice.Amount, r.OverridePrice.Currency,
                r.Priority, r.IsActive)
        ).FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("PriceRule", request.Id);
        return dto;
    }
}
