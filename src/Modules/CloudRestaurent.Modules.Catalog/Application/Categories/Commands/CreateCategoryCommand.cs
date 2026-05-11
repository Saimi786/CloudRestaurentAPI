using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Categories.Dtos;
using CloudRestaurent.Modules.Catalog.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Catalog.Application.Categories.Commands;

public sealed record CreateCategoryCommand(
    string Name,
    int DisplayOrder,
    Guid? ParentCategoryId = null,
    Guid? KitchenStationId = null) : IRequest<CategoryDto>;

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateCategoryHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        if (request.ParentCategoryId is { } pid &&
            !await db.Set<Category>().AnyAsync(c => c.Id == pid, ct))
            throw new NotFoundException("Parent Category", pid);

        // Uniqueness is per-level — same name allowed under different parents.
        if (await db.Set<Category>().AnyAsync(c =>
                c.Name == request.Name && c.ParentCategoryId == request.ParentCategoryId, ct))
            throw new ConflictException(
                $"A category named '{request.Name}' already exists at this level.");

        var category = new Category(Guid.NewGuid(), tenantId,
            request.Name, request.DisplayOrder, request.ParentCategoryId);
        category.SetKitchenStation(request.KitchenStationId);
        db.Set<Category>().Add(category);
        await db.SaveChangesAsync(ct);

        string? parentName = null;
        if (category.ParentCategoryId is { } parentId)
            parentName = await db.Set<Category>().AsNoTracking()
                .Where(c => c.Id == parentId).Select(c => c.Name).FirstOrDefaultAsync(ct);

        return new CategoryDto(
            category.Id, category.Name, category.DisplayOrder,
            category.ParentCategoryId, parentName,
            category.KitchenStationId,
            parentName is null ? 0 : 1,
            parentName is null ? category.Name : $"{parentName} > {category.Name}",
            category.IsActive);
    }
}
