using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Catalog.Application.Brands.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.Brands.Queries;

public sealed record GetBrandsQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<BrandDto>>;

public sealed class GetBrandsHandler(IAppDbContext db)
    : IRequestHandler<GetBrandsQuery, IReadOnlyList<BrandDto>>
{
    public async Task<IReadOnlyList<BrandDto>> Handle(GetBrandsQuery request, CancellationToken ct)
    {
        var query = db.Set<Brand>().AsNoTracking();
        if (!request.IncludeInactive)
            query = query.Where(b => b.IsActive);

        return await query
            .OrderBy(b => b.Name)
            .Select(b => new BrandDto(b.Id, b.Name, b.Description, b.ImageUrl, b.IsActive))
            .ToListAsync(ct);
    }
}
