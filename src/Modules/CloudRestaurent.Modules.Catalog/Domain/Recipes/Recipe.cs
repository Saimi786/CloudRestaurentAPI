using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Catalog.Domain.Recipes;

public class Recipe : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }

    /// <summary>The menu Product this recipe produces (e.g. "Classic Beef Burger").</summary>
    public Guid ProductId { get; private set; }

    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// How many portions one batch of this recipe yields. Ingredient deduction at sale time
    /// divides the ingredient quantity by this. Default 1 (recipe makes one portion per batch).
    /// </summary>
    public decimal BatchYield { get; private set; } = 1m;

    private readonly List<RecipeIngredient> _ingredients = new();
    public IReadOnlyCollection<RecipeIngredient> Ingredients => _ingredients;

    private readonly List<RecipeStep> _steps = new();
    public IReadOnlyCollection<RecipeStep> Steps => _steps;

    private Recipe() { }

    public Recipe(Guid id, Guid tenantId, Guid productId, string? notes, decimal? batchYield = null)
    {
        Id = id;
        TenantId = tenantId;
        ProductId = productId;
        Notes = notes;
        BatchYield = batchYield is { } y && y > 0 ? y : 1m;
        IsActive = true;
    }

    public void SetNotes(string? notes) => Notes = notes;

    public void SetBatchYield(decimal yield)
    {
        if (yield <= 0) throw new ArgumentOutOfRangeException(nameof(yield), "Yield must be > 0.");
        BatchYield = yield;
    }

    /// <summary>Replace the entire ingredient list. The caller has already validated each row.</summary>
    public void ReplaceIngredients(IEnumerable<RecipeIngredient> ingredients)
    {
        _ingredients.Clear();
        foreach (var i in ingredients) _ingredients.Add(i);
    }

    public void ReplaceSteps(IEnumerable<RecipeStep> steps)
    {
        _steps.Clear();
        foreach (var s in steps) _steps.Add(s);
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
