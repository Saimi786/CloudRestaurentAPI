namespace CloudRestaurent.Modules.Catalog.Application.Categories.Dtos;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    int DisplayOrder,
    Guid? ParentCategoryId,
    string? ParentName,
    Guid? KitchenStationId,
    int Depth,
    string FullPath,
    bool IsActive);
