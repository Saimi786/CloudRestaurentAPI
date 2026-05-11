using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Sales.Application.Common;
using CloudRestaurent.Modules.Sales.Application.Dtos;
using CloudRestaurent.Modules.Sales.Application.Promotions;
using CloudRestaurent.Modules.Catalog.Domain.Modifiers;
using CloudRestaurent.Domain.Common;
using CloudRestaurent.Modules.Sales.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;
using TaxRate = CloudRestaurent.Modules.Tax.Domain.TaxRate;

namespace CloudRestaurent.Modules.Sales.Application.Commands;

public sealed record AddOrderLineCommand(
    Guid OrderId,
    Guid ProductId,
    decimal Quantity,
    string? Notes,
    IReadOnlyList<Guid> ModifierIds) : IRequest<OrderDto>;

public sealed class AddOrderLineValidator : AbstractValidator<AddOrderLineCommand>
{
    public AddOrderLineValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.ModifierIds).NotNull();
    }
}

public sealed class AddOrderLineHandler(
    IAppDbContext db, IPriceResolver priceResolver, PromotionRecomputer promotions)
    : IRequestHandler<AddOrderLineCommand, OrderDto>
{
    public async Task<OrderDto> Handle(AddOrderLineCommand request, CancellationToken ct)
    {
        var order = await db.Set<Order>().FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (order.Status != OrderStatus.Open)
            throw new BusinessRuleException("Cannot add lines to a non-open order.");

        var product = await db.Set<Product>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new NotFoundException("Product", request.ProductId);

        if (!product.IsActive)
            throw new BusinessRuleException($"Product '{product.Name}' is inactive.");

        // Resolve price via the rule engine (branch + time aware)
        var resolved = await priceResolver.ResolveAsync(product.Id, order.BranchId, DateTime.Now, ct);

        if (resolved.Currency != order.Currency)
            throw new BusinessRuleException(
                $"Resolved price currency '{resolved.Currency}' does not match order currency '{order.Currency}'.");

        // Validate + snapshot any selected modifiers
        var lineId = Guid.NewGuid();
        var modifierIds = request.ModifierIds.Distinct().ToList();
        var modifiers = await db.Set<Modifier>().AsNoTracking()
            .Where(m => modifierIds.Contains(m.Id))
            .ToListAsync(ct);
        var missing = modifierIds.Except(modifiers.Select(m => m.Id)).ToList();
        if (missing.Count > 0)
            throw new NotFoundException("Modifier", string.Join(", ", missing));

        // Resolve the tax rate: non-taxable products → 0%; otherwise prefer the product's
        // explicit TaxRateId, then fall back to the tenant's default rate.
        Guid? appliedRateId = null;
        decimal appliedPercentage = 0m;
        if (product.IsTaxable)
        {
            TaxRate? rate = null;
            if (product.TaxRateId is { } pid)
                rate = await db.Set<TaxRate>().AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == pid && t.IsActive, ct);

            rate ??= await db.Set<TaxRate>().AsNoTracking()
                .FirstOrDefaultAsync(t => t.IsDefault && t.IsActive, ct);

            if (rate is not null)
            {
                appliedRateId = rate.Id;
                appliedPercentage = rate.Percentage;
            }
        }

        var modifiersTotal = modifiers.Sum(m => m.PriceAdjustment.Amount);

        var line = new OrderLine(
            lineId, order.Id,
            product.Id, product.Sku, product.Name,
            request.Quantity,
            new Money(resolved.Amount, resolved.Currency),
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes!.Trim());

        line.SnapshotTotals(modifiersTotal, appliedRateId, appliedPercentage);

        db.Set<OrderLine>().Add(line);

        foreach (var m in modifiers)
            db.Set<OrderLineModifier>().Add(new OrderLineModifier(
                Guid.NewGuid(), line.Id, m.Id, m.Name, m.PriceAdjustment));

        await db.SaveChangesAsync(ct);

        // Recompute order totals from the full set of persisted lines.
        var allLines = await db.Set<OrderLine>().AsNoTracking()
            .Where(l => l.OrderId == order.Id)
            .ToListAsync(ct);
        order.RecomputeTotals(allLines);

        // Mix & Match: re-evaluate active promotions against the new cart composition.
        // RecomputeTotals already cleared promotion via implicit zero — this rebuilds it.
        await promotions.RecomputeAsync(order, DateTime.Now, ct);
        await db.SaveChangesAsync(ct);

        return await OrderDtoBuilder.BuildAsync(db, order, ct);
    }
}
