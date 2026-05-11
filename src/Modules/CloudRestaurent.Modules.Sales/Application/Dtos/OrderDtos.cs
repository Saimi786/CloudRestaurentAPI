using CloudRestaurent.Modules.Sales.Domain;

namespace CloudRestaurent.Modules.Sales.Application.Dtos;

public sealed record OrderLineModifierDto(
    Guid Id,
    Guid ModifierId,
    string Name,
    decimal PriceAdjustmentAmount);

public sealed record OrderLineDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal Quantity,
    decimal UnitPriceAmount,
    string Currency,
    string? Notes,
    decimal Subtotal,
    Guid? TaxRateId,
    decimal TaxRatePercentage,
    decimal TaxAmount,
    decimal LineGrandTotal,
    IReadOnlyList<OrderLineModifierDto> Modifiers);

public sealed record PaymentDto(
    Guid Id,
    PaymentMethod Method,
    string MethodName,
    decimal Amount,
    string Currency,
    string? Reference,
    DateTimeOffset PaidAt);

public sealed record OrderPromotionDto(
    Guid Id,
    Guid SourceGroupId,
    string Name,
    string? Description,
    decimal Amount);

public sealed record OrderDto(
    Guid Id,
    string? OrderNumber,
    Guid BranchId,
    string BranchName,
    Guid? TableId,
    string? TableCode,
    Guid? CustomerId,
    string? CustomerName,
    OrderType Type,
    string TypeName,
    OrderStatus Status,
    string StatusName,
    string Currency,
    string? Notes,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    decimal SubtotalAmount,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal PromotionDiscountAmount,
    decimal GrandTotalAmount,
    decimal PaidTotal,
    decimal Balance,
    int RewardPointsEarned,
    int RewardPointsRedeemed,
    decimal RewardPointsRedeemedAmount,
    IReadOnlyList<OrderLineDto> Lines,
    IReadOnlyList<PaymentDto> Payments,
    IReadOnlyList<OrderPromotionDto> Promotions);

public sealed record OrderSummaryDto(
    Guid Id,
    string? OrderNumber,
    Guid BranchId,
    string BranchName,
    string? TableCode,
    string? CustomerName,
    OrderType Type,
    string TypeName,
    OrderStatus Status,
    string StatusName,
    string Currency,
    int LineCount,
    decimal SubtotalAmount,
    decimal TaxAmount,
    decimal DiscountAmount,
    decimal GrandTotalAmount,
    decimal Balance,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt);

public sealed record AddOrderLineModifierInput(Guid ModifierId);
