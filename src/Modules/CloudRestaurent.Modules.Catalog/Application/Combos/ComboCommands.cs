using CloudRestaurent.Application.Common.Abstractions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ComboComponent = CloudRestaurent.Modules.Catalog.Domain.ComboComponent;
using NotFoundException = CloudRestaurent.Application.Common.Exceptions.NotFoundException;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;
using ProductType = CloudRestaurent.Modules.Catalog.Domain.ProductType;
using UnauthorizedException = CloudRestaurent.Application.Common.Exceptions.UnauthorizedException;
using ValidationException = CloudRestaurent.Application.Common.Exceptions.ValidationException;

namespace CloudRestaurent.Modules.Catalog.Application.Combos;

public sealed record ComboComponentDto(
    Guid Id,
    Guid ComponentProductId,
    string ComponentSku,
    string ComponentName,
    decimal Quantity);

public sealed record ComboInput(Guid ComponentProductId, decimal Quantity);

/// <summary>
/// Replace the full set of components on a Combo product. Idempotent — caller
/// passes the desired final list and we delete-add as needed. Rejected if the
/// parent product isn't a Combo type, or if any component is itself a Combo
/// (we don't support nested combos in v1).
/// </summary>
public sealed record SetComboComponentsCommand(Guid ParentProductId, IReadOnlyList<ComboInput> Components)
    : IRequest<IReadOnlyList<ComboComponentDto>>;

public sealed class SetComboComponentsValidator : AbstractValidator<SetComboComponentsCommand>
{
    public SetComboComponentsValidator()
    {
        RuleFor(x => x.ParentProductId).NotEmpty();
        RuleFor(x => x.Components).NotNull();
        RuleForEach(x => x.Components).ChildRules(c =>
        {
            c.RuleFor(x => x.ComponentProductId).NotEmpty();
            c.RuleFor(x => x.Quantity).GreaterThan(0);
        });
    }
}

public sealed class SetComboComponentsHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<SetComboComponentsCommand, IReadOnlyList<ComboComponentDto>>
{
    public async Task<IReadOnlyList<ComboComponentDto>> Handle(SetComboComponentsCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var parent = await db.Set<Product>().FirstOrDefaultAsync(p => p.Id == request.ParentProductId, ct)
            ?? throw new NotFoundException("Product", request.ParentProductId);

        if (parent.Type != ProductType.Combo)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["parentProductId"] = ["Parent product must have Type = Combo."]
            });

        // Components must exist, must be active, must not themselves be combos.
        var componentIds = request.Components.Select(c => c.ComponentProductId).Distinct().ToList();
        var componentProducts = await db.Set<Product>().AsNoTracking()
            .Where(p => componentIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Type, p.Sku, p.Name })
            .ToListAsync(ct);

        var byId = componentProducts.ToDictionary(p => p.Id);
        foreach (var input in request.Components)
        {
            if (!byId.TryGetValue(input.ComponentProductId, out var c))
                throw new NotFoundException("Product", input.ComponentProductId);
            if (c.Type == ProductType.Combo)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    [$"components"] = [$"Component '{c.Name}' is a Combo — nested combos are not supported."]
                });
            if (c.Id == request.ParentProductId)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    [$"components"] = ["A combo cannot include itself."]
                });
        }

        // Replace all-or-nothing. Simpler than diffing and the table is tiny per combo.
        var existing = await db.Set<ComboComponent>()
            .Where(c => c.ParentProductId == request.ParentProductId)
            .ToListAsync(ct);
        db.Set<ComboComponent>().RemoveRange(existing);

        foreach (var input in request.Components)
        {
            db.Set<ComboComponent>().Add(new ComboComponent(
                Guid.NewGuid(), tenantId, request.ParentProductId,
                input.ComponentProductId, input.Quantity));
        }
        await db.SaveChangesAsync(ct);

        return request.Components.Select(input =>
        {
            var c = byId[input.ComponentProductId];
            return new ComboComponentDto(Guid.Empty, c.Id, c.Sku, c.Name, input.Quantity);
        }).ToList();
    }
}

public sealed record GetComboComponentsQuery(Guid ParentProductId) : IRequest<IReadOnlyList<ComboComponentDto>>;

public sealed class GetComboComponentsHandler(IAppDbContext db)
    : IRequestHandler<GetComboComponentsQuery, IReadOnlyList<ComboComponentDto>>
{
    public async Task<IReadOnlyList<ComboComponentDto>> Handle(GetComboComponentsQuery request, CancellationToken ct)
    {
        return await (
            from cc in db.Set<ComboComponent>().AsNoTracking()
            join p in db.Set<Product>().AsNoTracking() on cc.ComponentProductId equals p.Id
            where cc.ParentProductId == request.ParentProductId
            orderby p.Name
            select new ComboComponentDto(cc.Id, p.Id, p.Sku, p.Name, cc.Quantity))
            .ToListAsync(ct);
    }
}
