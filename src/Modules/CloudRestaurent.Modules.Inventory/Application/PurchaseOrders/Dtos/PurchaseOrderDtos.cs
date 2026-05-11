using CloudRestaurent.Modules.Inventory.Domain;

namespace CloudRestaurent.Modules.Inventory.Application.PurchaseOrders.Dtos;

public sealed record PurchaseOrderLineDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    Guid UnitId,
    string UnitCode,
    decimal OrderedQuantity,
    decimal ReceivedQuantity,
    decimal UnitCost,
    decimal LineTotal,
    string? Notes);

public sealed record PurchaseOrderDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    Guid SupplierId,
    string SupplierName,
    string Number,
    PurchaseOrderStatus Status,
    string StatusName,
    DateOnly? ExpectedDate,
    string Currency,
    string? Notes,
    decimal SubtotalAmount,
    decimal TaxAmount,
    decimal GrandTotalAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<PurchaseOrderLineDto> Lines);

public sealed record PurchaseOrderSummaryDto(
    Guid Id,
    string Number,
    string SupplierName,
    PurchaseOrderStatus Status,
    string StatusName,
    DateOnly? ExpectedDate,
    decimal GrandTotalAmount,
    string Currency,
    DateTimeOffset CreatedAt);

public sealed record PurchaseOrderLineInput(
    Guid ProductId,
    Guid UnitId,
    decimal OrderedQuantity,
    decimal UnitCost,
    string? Notes);

public sealed record ReceiveLineInput(
    Guid LineId,
    decimal Quantity);
