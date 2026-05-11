namespace CloudRestaurent.Modules.Catalog.Application.Brands.Dtos;

public sealed record BrandDto(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    bool IsActive);
