namespace CloudRestaurent.Modules.Tenancy.Application.Branches.Dtos;

public sealed record LocationDto(
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? Country,
    string? PostalCode,
    double? Latitude,
    double? Longitude,
    string TimeZone);

public sealed record BranchDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string Code,
    string? PhoneNumber,
    LocationDto Location,
    bool IsActive,
    int ReceiptTemplate,
    string? ReceiptFooterText);
