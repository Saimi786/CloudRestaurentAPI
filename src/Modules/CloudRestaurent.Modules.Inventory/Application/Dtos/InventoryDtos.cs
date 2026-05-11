using CloudRestaurent.Modules.Inventory.Domain;

namespace CloudRestaurent.Modules.Inventory.Application.Dtos;

public sealed record StockBalanceDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    string ProductUnitCode,
    decimal Quantity,
    DateTimeOffset LastMovementAt);

public sealed record StockMovementDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    StockMovementType Type,
    string TypeName,
    Guid UnitId,
    string UnitCode,
    decimal Quantity,
    decimal QuantityInProductUnit,
    string ProductUnitCode,
    string? Reference,
    string? Notes,
    DateTimeOffset OccurredAt,
    string? CreatedBy);
