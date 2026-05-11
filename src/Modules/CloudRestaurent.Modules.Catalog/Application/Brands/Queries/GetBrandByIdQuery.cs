using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Brands.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.Brands.Queries;

public sealed record GetBrandByIdQuery(Guid Id) : IRequest<BrandDto>;

public sealed class GetBrandByIdHandler(IAppDbContext db)
    : IRequestHandler<GetBrandByIdQuery, BrandDto>
{
    public async Task<BrandDto> Handle(GetBrandByIdQuery request, CancellationToken ct)
    {
        var dto = await db.Set<Brand>().AsNoTracking()
            .Where(b => b.Id == request.Id)
            .Select(b => new BrandDto(b.Id, b.Name, b.Description, b.ImageUrl, b.IsActive))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Brand", request.Id);
        return dto;
    }
}
