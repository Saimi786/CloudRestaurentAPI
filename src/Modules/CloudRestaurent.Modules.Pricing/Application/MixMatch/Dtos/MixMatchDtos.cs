using CloudRestaurent.Modules.Pricing.Domain;

namespace CloudRestaurent.Modules.Pricing.Application.MixMatch.Dtos;

public sealed record MixMatchProductDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal? CostPriceAmount,
    decimal RetailPriceAmount,
    string Currency,
    decimal? NetMarginPercent);

public sealed record MixMatchGroupDto(
    Guid Id,
    string Name,
    MixMatchType Type,
    string TypeName,
    int Quantity,
    decimal DiscountValue,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DaysOfWeekFlags DaysOfWeek,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int Priority,
    bool Stackable,
    int ProductCount,
    bool IsActive);

public sealed record MixMatchGroupDetailDto(
    Guid Id,
    string Name,
    MixMatchType Type,
    string TypeName,
    int Quantity,
    decimal DiscountValue,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DaysOfWeekFlags DaysOfWeek,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int Priority,
    bool Stackable,
    bool IsActive,
    IReadOnlyList<MixMatchProductDto> Products);
