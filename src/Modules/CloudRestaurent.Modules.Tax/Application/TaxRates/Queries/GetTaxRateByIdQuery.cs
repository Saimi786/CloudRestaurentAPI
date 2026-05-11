using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Tax.Application.TaxRates.Dtos;
using CloudRestaurent.Modules.Tax.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tax.Application.TaxRates.Queries;

public sealed record GetTaxRateByIdQuery(Guid Id) : IRequest<TaxRateDto>;

public sealed class GetTaxRateByIdHandler(IAppDbContext db)
    : IRequestHandler<GetTaxRateByIdQuery, TaxRateDto>
{
    public async Task<TaxRateDto> Handle(GetTaxRateByIdQuery request, CancellationToken ct)
    {
        var dto = await db.Set<TaxRate>().AsNoTracking()
            .Where(t => t.Id == request.Id)
            .Select(t => new TaxRateDto(t.Id, t.Name, t.Percentage, t.IsCompound, t.IsDefault, t.IsActive))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("TaxRate", request.Id);
        return dto;
    }
}
