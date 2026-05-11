using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Recipes.Dtos;
using CloudRestaurent.Modules.Catalog.Domain.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.Recipes.Queries;

public sealed record GetRecipeByProductIdQuery(Guid ProductId) : IRequest<RecipeDto>;

public sealed class GetRecipeByProductIdHandler(IAppDbContext db)
    : IRequestHandler<GetRecipeByProductIdQuery, RecipeDto>
{
    public async Task<RecipeDto> Handle(GetRecipeByProductIdQuery request, CancellationToken ct)
    {
        var recipe = await db.Set<Recipe>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.ProductId == request.ProductId, ct)
            ?? throw new NotFoundException("Recipe (for product)", request.ProductId);

        return await GetRecipeByIdHandler.BuildDtoAsync(db, recipe, ct);
    }
}
