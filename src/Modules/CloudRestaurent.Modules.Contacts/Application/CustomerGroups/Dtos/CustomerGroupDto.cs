namespace CloudRestaurent.Modules.Contacts.Application.CustomerGroups.Dtos;

public sealed record CustomerGroupDto(
    Guid Id,
    string Name,
    decimal DiscountPercent,
    string? Description,
    bool IsActive);
