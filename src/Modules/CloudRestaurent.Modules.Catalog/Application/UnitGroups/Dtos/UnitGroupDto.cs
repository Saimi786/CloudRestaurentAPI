namespace CloudRestaurent.Modules.Catalog.Application.UnitGroups.Dtos;

public sealed record UnitGroupDto(Guid Id, string Name, int UnitCount, bool IsActive);
