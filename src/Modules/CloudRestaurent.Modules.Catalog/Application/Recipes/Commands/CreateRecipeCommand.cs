using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Recipes.Common;
using CloudRestaurent.Modules.Catalog.Application.Recipes.Dtos;
using CloudRestaurent.Modules.Catalog.Application.Recipes.Queries;
using CloudRestaurent.Modules.Catalog.Domain.Recipes;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;

namespace CloudRestaurent.Modules.Catalog.Application.Recipes.Commands;

public sealed record CreateRecipeCommand(
    Guid ProductId,
    string? Notes,
    decimal BatchYield,
    IReadOnlyList<RecipeIngredientInput> Ingredients,
    IReadOnlyList<RecipeStepInput>? Steps) : IRequest<RecipeDto>;

public sealed class CreateRecipeValidator : AbstractValidator<CreateRecipeCommand>
{
    public CreateRecipeValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
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
            s.RuleFor(x => x.DurationMinutes).GreaterThanOrEqualTo(0).When(x => x.DurationMinutes.HasValue);
        }).When(x => x.Steps != null);
    }
}

public sealed class CreateRecipeHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<CreateRecipeCommand, RecipeDto>
{
    public async Task<RecipeDto> Handle(CreateRecipeCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var product = await db.Set<Product>().FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new NotFoundException("Product", request.ProductId);

        if (await db.Set<Recipe>().AnyAsync(r => r.ProductId == request.ProductId, ct))
            throw new ConflictException(
                $"A recipe for product '{product.Name}' already exists. Update it instead.");

        var recipeId = Guid.NewGuid();
        var ingredients = await RecipeIngredientValidator.ValidateAndBuildAsync(
            db, recipeId, request.Ingredients, ct);

        var recipe = new Recipe(recipeId, tenantId, request.ProductId,
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes!.Trim(),
            request.BatchYield);
        recipe.ReplaceIngredients(ingredients);

        if (request.Steps is { Count: > 0 })
        {
            var steps = request.Steps
                .OrderBy(s => s.StepNumber)
                .Select(s => new RecipeStep(Guid.NewGuid(), recipeId,
                    s.StepNumber, s.Instruction.Trim(), s.DurationMinutes))
                .ToList();
            recipe.ReplaceSteps(steps);
        }

        db.Set<Recipe>().Add(recipe);
        await db.SaveChangesAsync(ct);

        return await GetRecipeByIdHandler.BuildDtoAsync(db, recipe, ct);
    }
}
