using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Catalog.Domain.Recipes;

/// <summary>
/// One step of a multi-step prep procedure (e.g. "Marinate 30 min", "Sear 2 min per side").
/// Free-form text — ingredients still live on Recipe directly. Steps are display-only;
/// the kitchen reads them top-to-bottom by <see cref="StepNumber"/>.
/// </summary>
public class RecipeStep : Entity<Guid>
{
    public Guid RecipeId { get; private set; }
    public int StepNumber { get; private set; }
    public string Instruction { get; private set; } = null!;
    public int? DurationMinutes { get; private set; }

    private RecipeStep() { }

    public RecipeStep(Guid id, Guid recipeId, int stepNumber, string instruction, int? durationMinutes)
    {
        if (stepNumber < 1) throw new ArgumentOutOfRangeException(nameof(stepNumber));
        if (string.IsNullOrWhiteSpace(instruction)) throw new ArgumentException("Instruction is required.", nameof(instruction));
        if (durationMinutes is < 0) throw new ArgumentOutOfRangeException(nameof(durationMinutes));

        Id = id;
        RecipeId = recipeId;
        StepNumber = stepNumber;
        Instruction = instruction;
        DurationMinutes = durationMinutes;
    }
}
