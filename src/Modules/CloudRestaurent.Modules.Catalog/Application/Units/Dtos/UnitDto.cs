namespace CloudRestaurent.Modules.Catalog.Application.Units.Dtos;

public sealed record UnitDto(
    Guid Id,
    Guid GroupId,
    string GroupName,
    string Code,
    string Name,
    decimal ConversionFactor,
    bool IsBase,
    bool IsActive);
