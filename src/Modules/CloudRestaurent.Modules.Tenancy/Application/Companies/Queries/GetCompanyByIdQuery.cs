using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Tenancy.Application.Companies.Dtos;
using CloudRestaurent.Domain.Companies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tenancy.Application.Companies.Queries;

public sealed record GetCompanyByIdQuery(Guid Id) : IRequest<CompanyDto>;

public sealed class GetCompanyByIdHandler(IAppDbContext db)
    : IRequestHandler<GetCompanyByIdQuery, CompanyDto>
{
    public async Task<CompanyDto> Handle(GetCompanyByIdQuery request, CancellationToken ct)
    {
        var dto = await db.Set<Company>().AsNoTracking()
            .Where(c => c.Id == request.Id)
            .Select(c => new CompanyDto(c.Id, c.Name, c.LegalName, c.DefaultCurrency,
                c.TaxRegistrationNumber, c.IsActive))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Company", request.Id);

        return dto;
    }
}
