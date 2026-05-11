using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Catalog.Domain.Recipes;

public class RecipeIngredient : Entity<Guid>
{
    public Guid RecipeId { get; private set; }
    public Guid IngredientProductId { get; private set; }
    public Guid UnitId { get; private set; }
    public decimal Quantity { get; private set; }
    public string? Notes { get; private set; }

    private RecipeIngredient() { }

    public RecipeIngredient(
        Guid id,
        Guid recipeId,
        Guid ingredientProductId,
        Guid unitId,
        decimal quantity,
        string? notes)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

        Id = id;
        RecipeId = recipeId;
        IngredientProductId = ingredientProductId;
        UnitId = unitId;
        Quantity = quantity;
        Notes = notes;
    }
}
