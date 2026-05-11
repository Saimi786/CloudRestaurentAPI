using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Recipes.Dtos;
using CloudRestaurent.Modules.Catalog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Catalog.Application.Recipes.Common;

/// <summary>
/// Validates a list of ingredient inputs against the catalog and returns built domain objects.
/// Rules:
///   1. No duplicate ingredient products in one recipe.
///   2. Each ingredient product must exist and have IsStockTracked = true.
///   3. Each ingredient unit must exist and be in the same group as the ingredient product's unit.
///   4. Quantity > 0 (also enforced by the entity constructor).
/// </summary>
internal static class RecipeIngredientValidator
{
    public static async Task<List<RecipeIngredient>> ValidateAndBuildAsync(
        IAppDbContext db,
        Guid recipeId,
        IReadOnlyList<RecipeIngredientInput> inputs,
        CancellationToken ct)
    {
        var duplicates = inputs
            .GroupBy(i => i.IngredientProductId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicates.Count > 0)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["ingredients"] = ["The same ingredient product appears more than once."]
            });

        var productIds = inputs.Select(i => i.IngredientProductId).Distinct().ToList();
        var unitIds = inputs.Select(i => i.UnitId).Distinct().ToList();

        var products = await db.Set<Product>().AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var units = await db.Set<Unit>().AsNoTracking()
            .Where(u => unitIds.Contains(u.Id) || productIds.Contains(u.Id)) // also load product units
            .ToListAsync(ct);

        // Load each product's primary unit (if not already in `units`)
        var productPrimaryUnitIds = products.Values.Select(p => p.UnitId).Distinct().ToList();
        var allNeededUnitIds = unitIds.Concat(productPrimaryUnitIds).Distinct().ToList();
        var unitDict = await db.Set<Unit>().AsNoTracking()
            .Where(u => allNeededUnitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var built = new List<RecipeIngredient>(inputs.Count);
        foreach (var input in inputs)
        {
            if (!products.TryGetValue(input.IngredientProductId, out var product))
                throw new NotFoundException("Product (ingredient)", input.IngredientProductId);

            if (!product.IsStockTracked)
                throw new BusinessRuleException(
                    $"Ingredient '{product.Name}' must be stock-tracked. Enable 'Stock tracked' on the Product first.");

            if (!unitDict.TryGetValue(input.UnitId, out var unit))
                throw new NotFoundException("Unit", input.UnitId);

            if (!unitDict.TryGetValue(product.UnitId, out var productUnit))
                throw new NotFoundException("Unit", product.UnitId);

            if (unit.GroupId != productUnit.GroupId)
                throw new BusinessRuleException(
                    $"Unit '{unit.Code}' is not in the same group as the ingredient '{product.Name}' unit '{productUnit.Code}'. Cannot convert.");

            built.Add(new RecipeIngredient(
                Guid.NewGuid(), recipeId, input.IngredientProductId, input.UnitId, input.Quantity,
                string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes!.Trim()));
        }

        return built;
    }
}
