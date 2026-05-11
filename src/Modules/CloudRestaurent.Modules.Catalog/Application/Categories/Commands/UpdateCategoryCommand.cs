using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Categories.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.Categories.Commands;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    int DisplayOrder,
    Guid? ParentCategoryId = null,
    Guid? KitchenStationId = null) : IRequest<CategoryDto>;

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateCategoryHandler(IAppDbContext db)
    : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await db.Set<Category>().FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new NotFoundException("Category", request.Id);

        if (request.ParentCategoryId is { } pid)
        {
            if (pid == request.Id)
                throw new BusinessRuleException("A category cannot be its own parent.");

            if (!await db.Set<Category>().AnyAsync(c => c.Id == pid, ct))
                throw new NotFoundException("Parent Category", pid);

            // Cycle check — walk the proposed parent's ancestry; if we hit ourselves, reject.
            var cursor = pid;
            var depth = 0;
            while (depth < 50)
            {
                var nextParent = await db.Set<Category>().AsNoTracking()
                    .Where(c => c.Id == cursor)
                    .Select(c => c.ParentCategoryId).FirstOrDefaultAsync(ct);
                if (nextParent is null) break;
                if (nextParent == request.Id)
                    throw new BusinessRuleException("Cannot move a category under one of its own descendants.");
                cursor = nextParent.Value;
                depth++;
            }
        }

        if (await db.Set<Category>().AnyAsync(c =>
                c.Id != request.Id &&
                c.Name == request.Name &&
                c.ParentCategoryId == request.ParentCategoryId, ct))
            throw new ConflictException($"A category named '{request.Name}' already exists at this level.");

        category.Update(request.Name, request.DisplayOrder, request.ParentCategoryId);
        category.SetKitchenStation(request.KitchenStationId);
        await db.SaveChangesAsync(ct);

        string? parentName = null;
        if (category.ParentCategoryId is { } pid2)
            parentName = await db.Set<Category>().AsNoTracking()
                .Where(c => c.Id == pid2).Select(c => c.Name).FirstOrDefaultAsync(ct);

        return new CategoryDto(
            category.Id, category.Name, category.DisplayOrder,
            category.ParentCategoryId, parentName,
            category.KitchenStationId,
            parentName is null ? 0 : 1,
            parentName is null ? category.Name : $"{parentName} > {category.Name}",
            category.IsActive);
    }
}
