namespace CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Dtos;

public sealed record ModifierDto(
    Guid Id,
    string Name,
    decimal PriceAdjustmentAmount,
    string PriceAdjustmentCurrency,
    int DisplayOrder,
    bool IsDefault);

public sealed record ModifierGroupDto(
    Guid Id,
    string Name,
    bool IsRequired,
    int MinSelect,
    int MaxSelect,
    bool IsActive,
    IReadOnlyList<ModifierDto> Modifiers);

public sealed record ModifierGroupSummaryDto(
    Guid Id,
    string Name,
    bool IsRequired,
    int MinSelect,
    int MaxSelect,
    int ModifierCount,
    bool IsActive);

public sealed record ModifierInput(
    string Name,
    decimal PriceAdjustmentAmount,
    string PriceAdjustmentCurrency,
    int DisplayOrder,
    bool IsDefault);
