namespace CloudRestaurent.Modules.Catalog.Application.Recipes.Dtos;

public sealed record RecipeIngredientDto(
    Guid Id,
    Guid IngredientProductId,
    string IngredientProductSku,
    string IngredientProductName,
    Guid UnitId,
    string UnitCode,
    decimal Quantity,
    string? Notes);

public sealed record RecipeStepDto(
    Guid Id,
    int StepNumber,
    string Instruction,
    int? DurationMinutes);

public sealed record RecipeStepInput(
    int StepNumber,
    string Instruction,
    int? DurationMinutes);

public sealed record RecipeDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string? Notes,
    decimal BatchYield,
    bool IsActive,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    IReadOnlyList<RecipeStepDto> Steps);

public sealed record RecipeSummaryDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    int IngredientCount,
    bool IsActive);

public sealed record RecipeIngredientInput(
    Guid IngredientProductId,
    Guid UnitId,
    decimal Quantity,
    string? Notes);
