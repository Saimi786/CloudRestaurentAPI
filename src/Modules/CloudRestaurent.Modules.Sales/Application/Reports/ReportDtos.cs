namespace CloudRestaurent.Modules.Sales.Application.Reports;

public sealed record SalesByDayRow(DateOnly Day, int OrderCount, decimal Subtotal, decimal Tax, decimal Discount, decimal Grand);

public sealed record SalesSummaryDto(
    DateTimeOffset From,
    DateTimeOffset To,
    int OrderCount,
    decimal SubtotalTotal,
    decimal TaxTotal,
    decimal DiscountTotal,
    decimal GrandTotal,
    decimal RefundTotal,
    decimal NetRevenue,
    IReadOnlyList<SalesByDayRow> ByDay);

public sealed record TopProductRow(
    Guid ProductId, string Sku, string Name,
    decimal QuantitySold, decimal Revenue);

