using CloudRestaurent.Modules.Sales.Domain;

namespace CloudRestaurent.Modules.Sales.Application.CashRegisters.Dtos;

public sealed record CashRegisterDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    string Code,
    string Name,
    bool IsActive,
    Guid? ActiveShiftId);

public sealed record CashRegisterShiftMovementDto(
    Guid Id,
    ShiftMovementType Type,
    string TypeName,
    decimal Amount,
    Guid? SourceId,
    string? Reference,
    string? Notes,
    DateTime CreatedAt);

public sealed record CashRegisterShiftDto(
    Guid Id,
    Guid CashRegisterId,
    string CashRegisterCode,
    string CashRegisterName,
    Guid BranchId,
    string BranchName,
    Guid OpenedByUserId,
    string OpenedByUserName,
    DateTime OpenedAt,
    decimal OpeningAmount,
    string Currency,
    Guid? ClosedByUserId,
    string? ClosedByUserName,
    DateTime? ClosedAt,
    decimal? DeclaredClosingAmount,
    decimal? ExpectedClosingAmount,
    decimal? OverShortAmount,
    string? Notes,
    ShiftStatus Status,
    string StatusName,
    decimal SaleTotal,
    decimal RefundTotal,
    decimal PaidOutTotal,
    decimal CashInTotal,
    decimal CashOutTotal,
    IReadOnlyList<CashRegisterShiftMovementDto> Movements);

public sealed record CashRegisterShiftSummaryDto(
    Guid Id,
    Guid CashRegisterId,
    string CashRegisterCode,
    string OpenedByUserName,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    decimal OpeningAmount,
    decimal? DeclaredClosingAmount,
    decimal? OverShortAmount,
    ShiftStatus Status,
    string StatusName);
