namespace CloudRestaurent.Modules.Restaurant.Application.FloorPlans.Dtos;

public sealed record FloorPlanDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    string Name,
    int DisplayOrder,
    int TableCount,
    bool IsActive);
