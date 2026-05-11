using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Categories.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.Categories.Queries;

public sealed record GetCategoryByIdQuery(Guid Id) : IRequest<CategoryDto>;

public sealed class GetCategoryByIdHandler(IAppDbContext db)
    : IRequestHandler<GetCategoryByIdQuery, CategoryDto>
{
    public async Task<CategoryDto> Handle(GetCategoryByIdQuery request, CancellationToken ct)
    {
        var c = await db.Set<Category>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException("Category", request.Id);

        // Walk up the parent chain to compute full breadcrumb path. Cap depth at 50 in case of bad data.
        var pathParts = new List<string> { c.Name };
        string? parentName = null;
        var current = c;
        var depth = 0;
        while (current.ParentCategoryId is { } pid && depth < 50)
        {
            var parent = await db.Set<Category>().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == pid, ct);
            if (parent is null) break;
            if (depth == 0) parentName = parent.Name;
            pathParts.Insert(0, parent.Name);
            current = parent;
            depth++;
        }

        return new CategoryDto(
            c.Id, c.Name, c.DisplayOrder,
            c.ParentCategoryId, parentName,
            c.KitchenStationId,
            depth, string.Join(" > ", pathParts), c.IsActive);
    }
}
