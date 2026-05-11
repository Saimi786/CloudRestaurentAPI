namespace CloudRestaurent.Modules.Tax.Application.TaxRates.Dtos;

public sealed record TaxRateDto(
    Guid Id,
    string Name,
    decimal Percentage,
    bool IsCompound,
    bool IsDefault,
    bool IsActive);
