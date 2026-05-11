using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Sales.Application.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Contacts.Domain;
using CloudRestaurent.Modules.Restaurant.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Common;

internal static class OrderDtoBuilder
{
    public static async Task<OrderDto> BuildAsync(IAppDbContext db, Order order, CancellationToken ct)
    {
        var branch = await db.Set<Branch>().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == order.BranchId, ct)
            ?? throw new NotFoundException("Branch", order.BranchId);

        string? tableCode = null;
        if (order.TableId is { } tid)
            tableCode = await db.Set<RestaurantTable>().AsNoTracking()
                .Where(t => t.Id == tid).Select(t => t.Code).FirstOrDefaultAsync(ct);

        string? customerName = null;
        if (order.CustomerId is { } cid)
            customerName = await db.Set<Customer>().AsNoTracking()
                .Where(c => c.Id == cid).Select(c => c.FullName).FirstOrDefaultAsync(ct);

        // Lines + their modifier rows are loaded explicitly (no AutoInclude).
        var lines = await db.Set<OrderLine>().AsNoTracking()
            .Where(l => l.OrderId == order.Id)
            .ToListAsync(ct);
        var lineIds = lines.Select(l => l.Id).ToList();
        var modifiersByLine = (await db.Set<OrderLineModifier>().AsNoTracking()
            .Where(m => lineIds.Contains(m.OrderLineId))
            .ToListAsync(ct))
            .GroupBy(m => m.OrderLineId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var lineDtos = lines.Select(l =>
        {
            var mods = modifiersByLine.GetValueOrDefault(l.Id, []);
            return new OrderLineDto(
                l.Id, l.ProductId, l.ProductSku, l.ProductName,
                l.Quantity, l.UnitPrice.Amount, l.UnitPrice.Currency,
                l.Notes, l.LineSubtotal,
                l.TaxRateId, l.TaxRatePercentage, l.TaxAmount, l.LineGrandTotal,
                mods.Select(m => new OrderLineModifierDto(
                    m.Id, m.ModifierId, m.Name, m.PriceAdjustment.Amount)).ToList());
        }).ToList();

        var payments = await db.Set<Payment>().AsNoTracking()
            .Where(p => p.OrderId == order.Id)
            .OrderBy(p => p.PaidAt)
            .Select(p => new PaymentDto(
                p.Id, p.Method, p.Method.ToString(),
                p.Amount.Amount, p.Amount.Currency, p.Reference, p.PaidAt))
            .ToListAsync(ct);

        var paidTotal = payments.Sum(p => p.Amount);

        var promotions = await db.Set<OrderPromotion>().AsNoTracking()
            .Where(p => p.OrderId == order.Id)
            .Select(p => new OrderPromotionDto(p.Id, p.SourceGroupId, p.Name, p.Description, p.Amount))
            .ToListAsync(ct);

        return new OrderDto(
            order.Id, order.OrderNumber,
            order.BranchId, branch.Name,
            order.TableId, tableCode, order.CustomerId, customerName,
            order.Type, order.Type.ToString(),
            order.Status, order.Status.ToString(),
            order.Currency, order.Notes,
            order.OpenedAt, order.ClosedAt,
            order.SubtotalAmount, order.TaxAmount, order.DiscountAmount,
            order.PromotionDiscountAmount, order.GrandTotalAmount,
            paidTotal, order.GrandTotalAmount - paidTotal,
            order.RewardPointsEarned, order.RewardPointsRedeemed, order.RewardPointsRedeemedAmount,
            lineDtos, payments, promotions);
    }
}
