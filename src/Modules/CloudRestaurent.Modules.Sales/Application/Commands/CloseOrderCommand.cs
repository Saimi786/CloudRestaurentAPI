using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Tenants;
using CloudRestaurent.Modules.Sales.Application.Common;
using CloudRestaurent.Modules.Sales.Application.Dtos;
using CloudRestaurent.Modules.Sales.Application.Rewards;
using CloudRestaurent.Modules.Catalog.Domain;
using CloudRestaurent.Modules.Catalog.Domain.Recipes;
using CloudRestaurent.Modules.Contacts.Domain;
using CloudRestaurent.Modules.Inventory.Domain;
using CloudRestaurent.Modules.Restaurant.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Sales.Application.Commands;

public sealed record CloseOrderCommand(Guid OrderId) : IRequest<OrderDto>;

public sealed class CloseOrderHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    IKitchenNotifier kitchen,
    ILedgerPoster ledger)
    : IRequestHandler<CloseOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CloseOrderCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var order = await db.Set<Order>().FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (order.Status != OrderStatus.Open)
            throw new BusinessRuleException($"Order is {order.Status}; only Open orders can be closed.");

        // Lines + payments live in the DB (we add them via DbSet, not via the Order's nav
        // collection — workaround for the EF concurrency issue). Total lines + their
        // modifiers separately and combine in memory; SQL Server rejects a nested SUM that
        // references more than one outer column in one expression.
        var lines = await db.Set<OrderLine>().AsNoTracking()
            .Where(l => l.OrderId == order.Id)
            .Select(l => new { l.Id, l.Quantity, UnitPrice = l.UnitPrice.Amount })
            .ToListAsync(ct);

        if (lines.Count == 0)
            throw new BusinessRuleException("Cannot close an order with no lines.");

        var lineIds = lines.Select(l => l.Id).ToList();
        var modifierTotals = (await db.Set<OrderLineModifier>().AsNoTracking()
                .Where(m => lineIds.Contains(m.OrderLineId))
                .Select(m => new { m.OrderLineId, m.PriceAdjustment.Amount })
                .ToListAsync(ct))
            .GroupBy(m => m.OrderLineId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var grandTotal = lines.Sum(l =>
            (l.UnitPrice + modifierTotals.GetValueOrDefault(l.Id)) * l.Quantity);

        var paidTotal = await db.Set<Payment>().AsNoTracking()
            .Where(p => p.OrderId == order.Id)
            .SumAsync(p => (decimal?)p.Amount.Amount, ct) ?? 0m;

        if (paidTotal + 0.0001m < grandTotal)
            throw new BusinessRuleException(
                $"Order is not fully paid. Total {grandTotal:0.00}, paid {paidTotal:0.00}.");

        // All checks passed — flip status. Order.Close() also enforces these (defense in depth)
        // but its in-entity collection counts are zero, so we set the state directly via reflection
        // would be ugly; instead expose a simple method on Order that just changes status.
        order.MarkClosed();

        await DeductRecipeStockAsync(db, tenantId, order, ct);
        await FreeTableIfAssigned(db, order, ct);
        await AwardLoyaltyIfCustomerAttached(db, order, paidTotal, ct);
        var ticketId = await MarkKitchenTicketServed(db, order, ct);

        await db.SaveChangesAsync(ct);

        if (ticketId is { } tid)
            await kitchen.TicketChangedAsync(tenantId, order.BranchId, tid, ct);

        // Post the closed order to the general ledger (idempotent — safe to retry).
        await ledger.PostOrderClosedAsync(tenantId, order.Id, ct);

        return await OrderDtoBuilder.BuildAsync(db, order, ct);
    }

    private static async Task DeductRecipeStockAsync(
        IAppDbContext db, Guid tenantId, Order order, CancellationToken ct)
    {
        var rawLines = await db.Set<OrderLine>().AsNoTracking()
            .Where(l => l.OrderId == order.Id)
            .Select(l => new { l.ProductId, l.Quantity })
            .ToListAsync(ct);

        // Expand combo lines into their component products. A combo's BasePrice already
        // covered the bundle, but the components are what actually leave the kitchen — so
        // they're what should deduct stock. Non-combo lines pass through unchanged.
        var (lines, comboComponentIds) = await ExpandCombosAsync(
            db, rawLines.Select(l => (l.ProductId, l.Quantity)).ToList(), ct);

        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        var recipes = await db.Set<Recipe>().AsNoTracking()
            .Where(r => r.IsActive && productIds.Contains(r.ProductId))
            .ToListAsync(ct);
        var recipeByProduct = recipes.ToDictionary(r => r.ProductId);

        if (recipes.Count == 0) return;

        var recipeIds = recipes.Select(r => r.Id).ToList();
        var ingredients = await db.Set<RecipeIngredient>().AsNoTracking()
            .Where(i => recipeIds.Contains(i.RecipeId))
            .ToListAsync(ct);
        var ingredientsByRecipe = ingredients
            .GroupBy(i => i.RecipeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var unitIds = ingredients.Select(i => i.UnitId).Distinct().ToList();
        var ingredientProductIds = ingredients.Select(i => i.IngredientProductId).Distinct().ToList();
        var ingredientProducts = await db.Set<CloudRestaurent.Modules.Catalog.Domain.Product>().AsNoTracking()
            .Where(p => ingredientProductIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);
        var allUnitIds = unitIds
            .Concat(ingredientProducts.Values.Select(p => p.UnitId))
            .Distinct().ToList();
        var unitsById = await db.Set<Unit>().AsNoTracking()
            .Where(u => allUnitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        // Combo components that ARE stock-tracked but have no recipe still need to deduct.
        // This pass runs once per (component, qty) pair from combo expansion that has no
        // matching recipe — so a "Coke" component on a "Burger Combo" deducts a Coke even
        // though Coke isn't recipe-driven. Non-combo recipe-less lines stay on legacy
        // semantics (no deduction) to avoid surprising existing tenants mid-flight.
        var comboFallbackLines = lines
            .Where(l => comboComponentIds.Contains(l.ProductId)
                     && !recipeByProduct.ContainsKey(l.ProductId))
            .ToList();
        if (comboFallbackLines.Count > 0)
        {
            var fallbackProductIds = comboFallbackLines.Select(l => l.ProductId).Distinct().ToList();
            var fallbackProducts = await db.Set<Product>().AsNoTracking()
                .Where(p => fallbackProductIds.Contains(p.Id) && p.IsStockTracked)
                .ToDictionaryAsync(p => p.Id, ct);

            foreach (var line in comboFallbackLines)
            {
                if (!fallbackProducts.TryGetValue(line.ProductId, out var product)) continue;
                var totalDelta = -line.Quantity;
                var balance = await db.Set<StockBalance>()
                    .FirstOrDefaultAsync(b => b.BranchId == order.BranchId && b.ProductId == product.Id, ct);
                if (balance is null)
                {
                    balance = new StockBalance(Guid.NewGuid(), tenantId, order.BranchId, product.Id);
                    db.Set<StockBalance>().Add(balance);
                }
                balance.Apply(totalDelta, DateTimeOffset.UtcNow);

                db.Set<StockMovement>().Add(new StockMovement(
                    Guid.NewGuid(), tenantId, order.BranchId, product.Id, product.UnitId,
                    StockMovementType.Sale, -line.Quantity, totalDelta,
                    reference: $"ORDER-{order.Id:N}"[..14],
                    notes: "Combo component consumption",
                    occurredAt: DateTimeOffset.UtcNow));
            }
        }

        foreach (var line in lines)
        {
            if (!recipeByProduct.TryGetValue(line.ProductId, out var recipe)) continue;
            if (!ingredientsByRecipe.TryGetValue(recipe.Id, out var ingreds)) continue;

            // Divide ingredient qty by batch yield: a recipe that yields 4 portions and lists
            // 1kg flour means each portion costs 0.25kg. yield defaults to 1 (no scaling).
            var yield = recipe.BatchYield > 0 ? recipe.BatchYield : 1m;

            foreach (var ingred in ingreds)
            {
                if (!ingredientProducts.TryGetValue(ingred.IngredientProductId, out var product)) continue;
                if (!unitsById.TryGetValue(ingred.UnitId, out var fromUnit)) continue;
                if (!unitsById.TryGetValue(product.UnitId, out var prodUnit)) continue;

                var perItemInProductUnit = ingred.Quantity * fromUnit.ConversionFactor / prodUnit.ConversionFactor / yield;
                var totalDelta = -(perItemInProductUnit * line.Quantity);

                var balance = await db.Set<StockBalance>()
                    .FirstOrDefaultAsync(b => b.BranchId == order.BranchId && b.ProductId == product.Id, ct);
                if (balance is null)
                {
                    balance = new StockBalance(Guid.NewGuid(), tenantId, order.BranchId, product.Id);
                    db.Set<StockBalance>().Add(balance);
                }
                balance.Apply(totalDelta, DateTimeOffset.UtcNow);

                db.Set<StockMovement>().Add(new StockMovement(
                    Guid.NewGuid(), tenantId, order.BranchId, product.Id, ingred.UnitId,
                    StockMovementType.Sale, -ingred.Quantity * line.Quantity, totalDelta,
                    reference: $"ORDER-{order.Id:N}"[..14],
                    notes: "Recipe consumption for sold item",
                    occurredAt: DateTimeOffset.UtcNow));
            }
        }
    }

    /// <summary>
    /// Replace any line whose product is a Combo with one entry per component.
    /// Nested combos are rejected at component-set time, so a single pass is enough.
    /// Each component's quantity = combo's recipe-style component qty times the order
    /// line's qty. Returns both the expanded line list and the set of product IDs that
    /// arrived via combo-expansion — caller uses that set for the fallback stock pass
    /// (components without recipes still need to deduct, unlike normal menu items).
    /// </summary>
    private static async Task<(List<(Guid ProductId, decimal Quantity)> Lines, HashSet<Guid> ComboComponentIds)>
        ExpandCombosAsync(
            IAppDbContext db,
            List<(Guid ProductId, decimal Quantity)> rawLines,
            CancellationToken ct)
    {
        var productIds = rawLines.Select(l => l.ProductId).Distinct().ToList();
        var comboParents = await db.Set<Product>().AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && p.Type == ProductType.Combo)
            .Select(p => p.Id)
            .ToListAsync(ct);

        if (comboParents.Count == 0) return (rawLines, new HashSet<Guid>());

        var components = await db.Set<ComboComponent>().AsNoTracking()
            .Where(c => comboParents.Contains(c.ParentProductId))
            .ToListAsync(ct);
        var byParent = components
            .GroupBy(c => c.ParentProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var expanded = new List<(Guid ProductId, decimal Quantity)>(rawLines.Count);
        var componentIds = new HashSet<Guid>();
        foreach (var (productId, quantity) in rawLines)
        {
            if (byParent.TryGetValue(productId, out var parts))
            {
                foreach (var c in parts)
                {
                    expanded.Add((c.ComponentProductId, c.Quantity * quantity));
                    componentIds.Add(c.ComponentProductId);
                }
            }
            else
            {
                expanded.Add((productId, quantity));
            }
        }
        return (expanded, componentIds);
    }

    private static async Task FreeTableIfAssigned(IAppDbContext db, Order order, CancellationToken ct)
    {
        if (order.TableId is not { } tid) return;
        var table = await db.Set<RestaurantTable>().FirstOrDefaultAsync(t => t.Id == tid, ct);
        if (table is null) return;
        if (table.Status == TableStatus.Occupied) table.SetStatus(TableStatus.Available);
    }

    private static async Task AwardLoyaltyIfCustomerAttached(
        IAppDbContext db, Order order, decimal paidTotal, CancellationToken ct)
    {
        if (order.CustomerId is not { } cid) return;
        var customer = await db.Set<Customer>().FirstOrDefaultAsync(c => c.Id == cid && c.IsActive, ct);
        if (customer is null) return;

        // Reward points settings live on BusinessSettings (per-tenant). Load and run the
        // shared calculator so the math matches UltimatePOS's behavior exactly.
        var settings = await db.Set<BusinessSettings>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.TenantId == order.TenantId, ct);
        if (settings is null || !settings.RewardPointsEnabled) return;

        var pointsToEarn = RewardPointsCalculator.CalculateEarned(settings, order.GrandTotalAmount);
        if (pointsToEarn > 0)
        {
            customer.ApplyEarnedDelta(pointsToEarn);
            order.SetRewardPointsEarned(pointsToEarn);
        }
    }

    private static async Task<Guid?> MarkKitchenTicketServed(IAppDbContext db, Order order, CancellationToken ct)
    {
        var ticket = await db.Set<KitchenTicket>().FirstOrDefaultAsync(t => t.OrderId == order.Id, ct);
        if (ticket is null) return null;
        ticket.MarkServed();
        return ticket.Id;
    }
}
