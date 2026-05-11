using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Tenancy.Application.Branches.Dtos;
using CloudRestaurent.Domain.Companies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tenancy.Application.Branches.Queries;

public sealed record GetBranchByIdQuery(Guid Id) : IRequest<BranchDto>;

public sealed class GetBranchByIdHandler(IAppDbContext db)
    : IRequestHandler<GetBranchByIdQuery, BranchDto>
{
    public async Task<BranchDto> Handle(GetBranchByIdQuery request, CancellationToken ct)
    {
        var dto = await db.Set<Branch>().AsNoTracking()
            .Where(b => b.Id == request.Id)
            .Select(b => new BranchDto(
                b.Id, b.CompanyId, b.Name, b.Code, b.PhoneNumber,
                new LocationDto(
                    b.Location.AddressLine1, b.Location.AddressLine2,
                    b.Location.City, b.Location.State, b.Location.Country,
                    b.Location.PostalCode, b.Location.Latitude, b.Location.Longitude,
                    b.Location.TimeZone),
                b.IsActive,
                (int)b.ReceiptTemplate,
                b.ReceiptFooterText))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Branch", request.Id);

        return dto;
    }
}
