using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Recipes.Common;
using CloudRestaurent.Modules.Catalog.Application.Recipes.Dtos;
using CloudRestaurent.Modules.Catalog.Application.Recipes.Queries;
using CloudRestaurent.Modules.Catalog.Domain.Recipes;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.Recipes.Commands;

public sealed record UpdateRecipeCommand(
    Guid Id,
    string? Notes,
    decimal BatchYield,
    IReadOnlyList<RecipeIngredientInput> Ingredients,
    IReadOnlyList<RecipeStepInput>? Steps) : IRequest<RecipeDto>;

public sealed class UpdateRecipeValidator : AbstractValidator<UpdateRecipeCommand>
{
    public UpdateRecipeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.BatchYield).GreaterThan(0);
        RuleFor(x => x.Ingredients).NotNull().NotEmpty().WithMessage("A recipe must have at least one ingredient.");
        RuleForEach(x => x.Ingredients).ChildRules(i =>
        {
            i.RuleFor(x => x.IngredientProductId).NotEmpty();
            i.RuleFor(x => x.UnitId).NotEmpty();
            i.RuleFor(x => x.Quantity).GreaterThan(0);
            i.RuleFor(x => x.Notes).MaximumLength(500);
        });
        RuleForEach(x => x.Steps!).ChildRules(s =>
        {
            s.RuleFor(x => x.StepNumber).GreaterThan(0);
            s.RuleFor(x => x.Instruction).NotEmpty().MaximumLength(1000);
        }).When(x => x.Steps != null);
    }
}

public sealed class UpdateRecipeHandler(IAppDbContext db) : IRequestHandler<UpdateRecipeCommand, RecipeDto>
{
    public async Task<RecipeDto> Handle(UpdateRecipeCommand request, CancellationToken ct)
    {
        var recipe = await db.Set<Recipe>().FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException("Recipe", request.Id);

        // Bulk-delete old child rows in SQL (avoids change-tracker conflicts).
        await db.Set<RecipeIngredient>().Where(i => i.RecipeId == recipe.Id).ExecuteDeleteAsync(ct);
        await db.Set<RecipeStep>().Where(s => s.RecipeId == recipe.Id).ExecuteDeleteAsync(ct);

        var newIngredients = await RecipeIngredientValidator.ValidateAndBuildAsync(
            db, recipe.Id, request.Ingredients, ct);
        db.Set<RecipeIngredient>().AddRange(newIngredients);

        if (request.Steps is { Count: > 0 })
        {
            var steps = request.Steps
                .OrderBy(s => s.StepNumber)
                .Select(s => new RecipeStep(Guid.NewGuid(), recipe.Id,
                    s.StepNumber, s.Instruction.Trim(), s.DurationMinutes))
                .ToList();
            db.Set<RecipeStep>().AddRange(steps);
        }

        recipe.SetNotes(string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes!.Trim());
        recipe.SetBatchYield(request.BatchYield);

        await db.SaveChangesAsync(ct);

        return await GetRecipeByIdHandler.BuildDtoAsync(db, recipe, ct);
    }
}
