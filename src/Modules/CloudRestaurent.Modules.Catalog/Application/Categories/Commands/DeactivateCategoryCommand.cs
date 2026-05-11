using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.Categories.Commands;

public sealed record DeactivateCategoryCommand(Guid Id) : IRequest;

public sealed class DeactivateCategoryHandler(IAppDbContext db) : IRequestHandler<DeactivateCategoryCommand>
{
    public async Task Handle(DeactivateCategoryCommand request, CancellationToken ct)
    {
        var category = await db.Set<Category>().FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new NotFoundException("Category", request.Id);
        if (!category.IsActive) return;

        if (await db.Set<Product>().AnyAsync(p => p.CategoryId == request.Id && p.IsActive, ct))
            throw new BusinessRuleException(
                "Cannot deactivate a category that still has active products. Move or deactivate the products first.");

        if (await db.Set<Category>().AnyAsync(c => c.ParentCategoryId == request.Id && c.IsActive, ct))
            throw new BusinessRuleException(
                "Cannot deactivate a category that still has active sub-categories. Deactivate the children first.");

        category.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
