using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Sales.Domain;

public class OrderLine : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;       // snapshot at line-add time
    public string ProductSku { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public Money UnitPrice { get; private set; }                   // resolved via IPriceResolver
    public string? Notes { get; private set; }

    /// <summary>Snapshot: qty × (unitPrice + sum(modifier price adjustments)). Excludes tax.</summary>
    public decimal LineSubtotal { get; private set; }

    /// <summary>Snapshot of the tax rate applied at line-add time (NULL if product was non-taxable).</summary>
    public Guid? TaxRateId { get; private set; }
    public decimal TaxRatePercentage { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal LineGrandTotal { get; private set; }

    private readonly List<OrderLineModifier> _modifiers = new();
    public IReadOnlyCollection<OrderLineModifier> Modifiers => _modifiers;

    private OrderLine() { }

    public OrderLine(
        Guid id, Guid orderId,
        Guid productId, string productSku, string productName,
        decimal quantity, Money unitPrice, string? notes)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be > 0.");

        Id = id;
        OrderId = orderId;
        ProductId = productId;
        ProductSku = productSku;
        ProductName = productName;
        Quantity = quantity;
        UnitPrice = unitPrice;
        Notes = notes;
    }

    public void AddModifier(OrderLineModifier modifier) => _modifiers.Add(modifier);

    /// <summary>
    /// Replace the resolved unit price with a manager-approved override. Caller must
    /// have re-snapshotted totals after the change — this method only mutates the price.
    /// </summary>
    public void OverrideUnitPrice(Money newPrice)
    {
        if (newPrice.Currency != UnitPrice.Currency)
            throw new InvalidOperationException(
                $"Cannot change line currency from {UnitPrice.Currency} to {newPrice.Currency}.");
        if (newPrice.Amount < 0)
            throw new ArgumentOutOfRangeException(nameof(newPrice), "Override price cannot be negative.");
        UnitPrice = newPrice;
    }

    /// <summary>
    /// Snapshot the line totals — call AFTER modifiers and tax info are known so the
    /// stored values match what the customer sees on the receipt.
    /// </summary>
    public void SnapshotTotals(decimal modifiersTotal, Guid? taxRateId, decimal taxRatePercentage)
    {
        var perUnit = UnitPrice.Amount + modifiersTotal;
        LineSubtotal = Math.Round(perUnit * Quantity, 4, MidpointRounding.AwayFromZero);
        TaxRateId = taxRateId;
        TaxRatePercentage = taxRatePercentage;
        TaxAmount = Math.Round(LineSubtotal * taxRatePercentage / 100m, 4, MidpointRounding.AwayFromZero);
        LineGrandTotal = LineSubtotal + TaxAmount;
    }
}
