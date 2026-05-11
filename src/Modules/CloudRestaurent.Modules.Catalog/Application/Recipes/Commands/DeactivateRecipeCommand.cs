using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Domain.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.Recipes.Commands;

public sealed record DeactivateRecipeCommand(Guid Id) : IRequest;

public sealed class DeactivateRecipeHandler(IAppDbContext db) : IRequestHandler<DeactivateRecipeCommand>
{
    public async Task Handle(DeactivateRecipeCommand request, CancellationToken ct)
    {
        var recipe = await db.Set<Recipe>().FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException("Recipe", request.Id);

        if (!recipe.IsActive) return;
        recipe.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
