namespace CloudRestaurent.Modules.Restaurant.Application.KitchenStations.Dtos;

public sealed record KitchenStationDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    string Name,
    int DisplayOrder,
    string? Description,
    bool IsActive,
    string? PrinterIpAddress = null,
    int? PrinterPort = null);
