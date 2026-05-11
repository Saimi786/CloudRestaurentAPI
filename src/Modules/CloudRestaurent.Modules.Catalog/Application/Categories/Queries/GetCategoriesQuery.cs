using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Catalog.Application.Categories.Common;
using CloudRestaurent.Modules.Catalog.Application.Categories.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.Categories.Queries;

public sealed record GetCategoriesQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<CategoryDto>>;

public sealed class GetCategoriesHandler(IAppDbContext db)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var query = db.Set<Category>().AsNoTracking();
        if (!request.IncludeInactive)
            query = query.Where(c => c.IsActive);

        var all = await query.ToListAsync(ct);
        return CategoryTreeBuilder.BuildOrdered(all);
    }
}
