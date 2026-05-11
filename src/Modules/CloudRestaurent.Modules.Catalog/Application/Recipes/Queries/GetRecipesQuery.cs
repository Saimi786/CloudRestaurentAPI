using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Catalog.Application.Recipes.Dtos;
using CloudRestaurent.Modules.Catalog.Domain.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;

namespace CloudRestaurent.Modules.Catalog.Application.Recipes.Queries;

public sealed record GetRecipesQuery(bool IncludeInactive = false)
    : IRequest<IReadOnlyList<RecipeSummaryDto>>;

public sealed class GetRecipesHandler(IAppDbContext db)
    : IRequestHandler<GetRecipesQuery, IReadOnlyList<RecipeSummaryDto>>
{
    public async Task<IReadOnlyList<RecipeSummaryDto>> Handle(GetRecipesQuery request, CancellationToken ct)
    {
        var recipes = db.Set<Recipe>().AsNoTracking();
        if (!request.IncludeInactive) recipes = recipes.Where(r => r.IsActive);

        return await (
            from r in recipes
            join p in db.Set<Product>().AsNoTracking() on r.ProductId equals p.Id
            orderby p.Name
            select new RecipeSummaryDto(
                r.Id, r.ProductId, p.Sku, p.Name,
                db.Set<RecipeIngredient>().Count(i => i.RecipeId == r.Id),
                r.IsActive)
        ).ToListAsync(ct);
    }
}
