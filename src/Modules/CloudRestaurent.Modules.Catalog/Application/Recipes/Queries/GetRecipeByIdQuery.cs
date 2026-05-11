using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Recipes.Dtos;
using CloudRestaurent.Modules.Catalog.Domain.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Catalog.Application.Recipes.Queries;

public sealed record GetRecipeByIdQuery(Guid Id) : IRequest<RecipeDto>;

public sealed class GetRecipeByIdHandler(IAppDbContext db)
    : IRequestHandler<GetRecipeByIdQuery, RecipeDto>
{
    public async Task<RecipeDto> Handle(GetRecipeByIdQuery request, CancellationToken ct)
    {
        var recipe = await db.Set<Recipe>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException("Recipe", request.Id);

        return await BuildDtoAsync(db, recipe, ct);
    }

    /// <summary>
    /// Build the full DTO by querying RecipeIngredients explicitly (rather than relying on
    /// the navigation collection being auto-included). Works for both freshly-fetched and
    /// just-saved Recipe instances.
    /// </summary>
    internal static async Task<RecipeDto> BuildDtoAsync(IAppDbContext db, Recipe recipe, CancellationToken ct)
    {
        var product = await db.Set<Product>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == recipe.ProductId, ct)
            ?? throw new NotFoundException("Product", recipe.ProductId);

        var ingredients = await db.Set<RecipeIngredient>().AsNoTracking()
            .Where(i => i.RecipeId == recipe.Id)
            .ToListAsync(ct);

        var ingredientProductIds = ingredients.Select(i => i.IngredientProductId).Distinct().ToList();
        var unitIds = ingredients.Select(i => i.UnitId).Distinct().ToList();

        var ingredientProducts = await db.Set<Product>().AsNoTracking()
            .Where(p => ingredientProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var units = await db.Set<Unit>().AsNoTracking()
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var ingredientDtos = ingredients
            .Select(i =>
            {
                var ip = ingredientProducts.GetValueOrDefault(i.IngredientProductId);
                var u = units.GetValueOrDefault(i.UnitId);
                return new RecipeIngredientDto(
                    i.Id, i.IngredientProductId,
                    ip?.Sku ?? "?", ip?.Name ?? "?",
                    i.UnitId, u?.Code ?? "?",
                    i.Quantity, i.Notes);
            })
            .ToList();

        var stepDtos = await db.Set<RecipeStep>().AsNoTracking()
            .Where(s => s.RecipeId == recipe.Id)
            .OrderBy(s => s.StepNumber)
            .Select(s => new RecipeStepDto(s.Id, s.StepNumber, s.Instruction, s.DurationMinutes))
            .ToListAsync(ct);

        return new RecipeDto(
            recipe.Id, recipe.ProductId, product.Sku, product.Name,
            recipe.Notes, recipe.BatchYield, recipe.IsActive, ingredientDtos, stepDtos);
    }
}
