using CloudRestaurent.Modules.Restaurant.Domain;

namespace CloudRestaurent.Modules.Restaurant.Application.Tables.Dtos;

public sealed record TableDto(
    Guid Id,
    Guid FloorPlanId,
    string FloorPlanName,
    Guid BranchId,
    string BranchName,
    string Code,
    int Capacity,
    TableStatus Status,
    string StatusName,
    bool IsActive);
