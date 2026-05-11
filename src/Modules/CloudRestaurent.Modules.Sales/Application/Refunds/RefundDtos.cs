using CloudRestaurent.Modules.Sales.Domain;

namespace CloudRestaurent.Modules.Sales.Application.Refunds;

public sealed record RefundLineDto(
    Guid Id, Guid OrderLineId, Guid ProductId, string ProductName, decimal Quantity, bool Restock);

public sealed record RefundDto(
    Guid Id,
    Guid OrderId,
    string? OrderNumber,
    Guid BranchId,
    Guid RefundedByUserId,
    string RefundedByUserName,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    string MethodName,
    string? Reason,
    DateTimeOffset RefundedAt,
    IReadOnlyList<RefundLineDto> Lines);

public sealed record RefundLineInput(Guid OrderLineId, decimal Quantity, bool Restock);
