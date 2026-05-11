using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Tax.Application.TaxRates.Dtos;
using CloudRestaurent.Modules.Tax.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tax.Application.TaxRates.Queries;

public sealed record GetTaxRatesQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<TaxRateDto>>;

public sealed class GetTaxRatesHandler(IAppDbContext db)
    : IRequestHandler<GetTaxRatesQuery, IReadOnlyList<TaxRateDto>>
{
    public async Task<IReadOnlyList<TaxRateDto>> Handle(GetTaxRatesQuery request, CancellationToken ct)
    {
        var query = db.Set<TaxRate>().AsNoTracking();
        if (!request.IncludeInactive) query = query.Where(t => t.IsActive);

        return await query
            .OrderByDescending(t => t.IsDefault).ThenBy(t => t.Name)
            .Select(t => new TaxRateDto(t.Id, t.Name, t.Percentage, t.IsCompound, t.IsDefault, t.IsActive))
            .ToListAsync(ct);
    }
}
