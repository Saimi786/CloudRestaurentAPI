using CloudRestaurent.Modules.Pricing.Domain;

namespace CloudRestaurent.Modules.Pricing.Application.Dtos;

public sealed record PriceRuleDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    Guid? BranchId,
    string? BranchName,
    string Name,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    DaysOfWeekFlags DaysOfWeek,
    decimal OverridePriceAmount,
    string OverridePriceCurrency,
    int Priority,
    bool IsActive);

public sealed record ResolvedPriceDto(
    decimal Amount,
    string Currency,
    Guid? AppliedRuleId,
    string? AppliedRuleName);
