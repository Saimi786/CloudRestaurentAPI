using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Tenancy.Application.Companies.Dtos;
using CloudRestaurent.Domain.Companies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tenancy.Application.Companies.Queries;

public sealed record GetCompaniesQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<CompanyDto>>;

public sealed class GetCompaniesHandler(IAppDbContext db)
    : IRequestHandler<GetCompaniesQuery, IReadOnlyList<CompanyDto>>
{
    public async Task<IReadOnlyList<CompanyDto>> Handle(GetCompaniesQuery request, CancellationToken ct)
    {
        var query = db.Set<Company>().AsNoTracking();
        if (!request.IncludeInactive)
            query = query.Where(c => c.IsActive);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new CompanyDto(c.Id, c.Name, c.LegalName, c.DefaultCurrency,
                c.TaxRegistrationNumber, c.IsActive))
            .ToListAsync(ct);
    }
}
