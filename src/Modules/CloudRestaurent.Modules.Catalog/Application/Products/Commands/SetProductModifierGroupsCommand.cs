using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Domain.Modifiers;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;

namespace CloudRestaurent.Modules.Catalog.Application.Products.Commands;

public sealed record SetProductModifierGroupsCommand(
    Guid ProductId,
    IReadOnlyList<Guid> ModifierGroupIds) : IRequest;

public sealed class SetProductModifierGroupsValidator : AbstractValidator<SetProductModifierGroupsCommand>
{
    public SetProductModifierGroupsValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ModifierGroupIds).NotNull();
    }
}

public sealed class SetProductModifierGroupsHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<SetProductModifierGroupsCommand>
{
    public async Task Handle(SetProductModifierGroupsCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        if (!await db.Set<Product>().AnyAsync(p => p.Id == request.ProductId, ct))
            throw new NotFoundException("Product", request.ProductId);

        // Validate all referenced modifier groups exist + are active
        var requestedIds = request.ModifierGroupIds.Distinct().ToList();
        var existing = await db.Set<ModifierGroup>().AsNoTracking()
            .Where(g => requestedIds.Contains(g.Id))
            .Select(g => new { g.Id, g.IsActive })
            .ToListAsync(ct);

        var missing = requestedIds.Except(existing.Select(e => e.Id)).ToList();
        if (missing.Count > 0)
            throw new NotFoundException("ModifierGroup", string.Join(", ", missing));

        var inactive = existing.Where(e => !e.IsActive).Select(e => e.Id).ToList();
        if (inactive.Count > 0)
            throw new BusinessRuleException(
                $"Cannot attach inactive modifier group(s): {string.Join(", ", inactive)}.");

        // Replace the link list: wipe + add. Atomic via single SaveChanges.
        await db.Set<ProductModifierGroup>()
            .Where(p => p.ProductId == request.ProductId)
            .ExecuteDeleteAsync(ct);

        var i = 0;
        foreach (var groupId in requestedIds)
        {
            db.Set<ProductModifierGroup>().Add(new ProductModifierGroup(
                Guid.NewGuid(), tenantId, request.ProductId, groupId, displayOrder: i++));
        }
        await db.SaveChangesAsync(ct);
    }
}
